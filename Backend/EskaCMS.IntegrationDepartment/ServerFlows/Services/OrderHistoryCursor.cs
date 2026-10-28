using System.Diagnostics;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Services
{
    /// <summary>One move of an order into a status, as a sweep reads it (never the order's snapshot the row also holds).</summary>
    internal sealed record OrderChange(long Id, long OrderId, long SiteId, OrderStatus? OldStatus, OrderStatus NewStatus, DateTimeOffset CreatedOn);

    /// <summary>
    /// Where a sweep of the orders' status history got to in this process. The first sweep reads back
    /// to a moment (the look-back); every later one only the rows added since, by their id (the
    /// table's own key), and never older than the look-back, so a sweep never scans the history
    /// again. A few ids before the mark are read again (rows written at the same moment can show up
    /// out of order), but a move already handed over is not handed over twice. A sweep reads batch
    /// after batch until it has caught up, for at most a few seconds. When the statuses asked for
    /// grow (a flow is switched on for another status), the history is read back to the look-back
    /// once more, for the new status.
    /// </summary>
    internal sealed class OrderHistoryCursor
    {
        private const int Overlap = 200;
        private const int Batch = 500;
        private static readonly TimeSpan TimeBox = TimeSpan.FromSeconds(10);

        private long? _mark;
        private HashSet<OrderStatus> _statuses = new();

        /// <summary>The moves handed over whose ids are still within the overlap (the ones a sweep may read again).</summary>
        private readonly HashSet<long> _handled = new();

        /// <summary>
        /// Hands the moves into one of <paramref name="statuses"/> since <paramref name="since"/> that
        /// are new to this process to <paramref name="handle"/>, oldest first, a batch at a time.
        /// </summary>
        public async Task ReadAsync(IRepository<OrderHistory> history, IReadOnlyCollection<OrderStatus> statuses, DateTimeOffset since,
            Func<IReadOnlyList<OrderChange>, Task> handle, CancellationToken ct)
        {
            if (!statuses.All(_statuses.Contains))
            {
                _statuses = statuses.ToHashSet();
                _mark = null;
            }
            var wanted = statuses.ToList();
            var clock = Stopwatch.StartNew();
            while (!ct.IsCancellationRequested)
            {
                var query = history.Query().AsNoTracking().Where(h => wanted.Contains(h.NewStatus) && h.CreatedOn >= since);
                if (_mark is long mark)
                {
                    var from = mark - Overlap;
                    query = query.Where(h => h.Id > from);
                }
                var read = await query
                    .OrderBy(h => h.Id)
                    .Take(Batch)
                    .Select(h => new OrderChange(h.Id, h.OrderId, h.Order.SiteId, h.OldStatus, h.NewStatus, h.CreatedOn))
                    .ToListAsync(ct);
                if (read.Count == 0)
                {
                    // Nothing (more) since: the next sweeps go on from the history's end, by id.
                    _mark ??= await history.Query().MaxAsync(h => (long?)h.Id, ct) ?? 0;
                    return;
                }

                var fresh = read.Where(c => !_handled.Contains(c.Id)).ToList();
                if (fresh.Count > 0)
                    await handle(fresh);
                foreach (var change in fresh)
                    _handled.Add(change.Id);
                _mark = Math.Max(_mark ?? 0, read[^1].Id);
                var keepFrom = _mark.Value - Overlap;
                _handled.RemoveWhere(id => id <= keepFrom);

                if (read.Count < Batch || clock.Elapsed > TimeBox)
                    return;
            }
        }
    }
}
