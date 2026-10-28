using System.Collections.Concurrent;
using EskaCMS.Entities.Entity.ServerFlows;
using EskaCMS.Infrastructure.Data;
using EskaCMS.IntegrationDepartment.ServerFlows.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Services
{
    /// <summary>
    /// The background job that runs server flows inside the web application.
    ///
    ///  - Runs queued in this process start at once (the order-paid handler queues them).
    ///  - Every SweepSeconds it reads the database for due runs (retries, settle waits, runs whose
    ///    worker died), for paid orders that missed their trigger, for scheduled flows whose time
    ///    has come, and for orders that moved to a status a flow waits for. This is what makes runs
    ///    survive restarts: nothing lives only in memory.
    ///  - At most WorkerConcurrency runs at once; a run already running here is not started twice
    ///    (handed over meanwhile, it is handed over again once its pickup ends).
    ///  - On shutdown it stops taking runs; the ones in progress stop between steps (and between the
    ///    items of a list) and continue on the next start.
    ///  - With RunWorker false this process only CREATES runs (a page, a webhook, a payment) and
    ///    neither calls anything nor sweeps, so the work can be left to one server (a batch server)
    ///    while the web servers keep taking orders. Any number of processes may have it true: a run
    ///    is claimed with a lease before it starts.
    ///
    /// IIS: give the site's app pool "AlwaysRunning" and preload, or an idle pool stops the worker
    /// until the next request (runs then resume late, never twice).
    /// </summary>
    public sealed class ServerFlowWorker : BackgroundService
    {
        private const int DueBatch = 100;

        private readonly IServiceScopeFactory _scopes;
        private readonly IOptionsMonitor<ServerFlowOptions> _options;
        private readonly ServerFlowQueue _queue;
        private readonly ServerFlowRunner _runner;
        private readonly ServerFlowTriggers _triggers;
        private readonly ServerFlowSchedules _schedules;
        private readonly ServerFlowOrderStatuses _orderStatuses;
        private readonly ServerFlowRetention _retention;
        private readonly ILogger<ServerFlowWorker> _logger;
        private readonly ConcurrentDictionary<long, Task> _inFlight = new();
        private readonly ConcurrentDictionary<long, byte> _again = new();
        private readonly string _workerId = Truncate($"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}", 100);

        public ServerFlowWorker(
            IServiceScopeFactory scopes,
            IOptionsMonitor<ServerFlowOptions> options,
            ServerFlowQueue queue,
            ServerFlowRunner runner,
            ServerFlowTriggers triggers,
            ServerFlowSchedules schedules,
            ServerFlowOrderStatuses orderStatuses,
            ServerFlowRetention retention,
            ILogger<ServerFlowWorker> logger)
        {
            _scopes = scopes;
            _options = options;
            _queue = queue;
            _runner = runner;
            _triggers = triggers;
            _schedules = schedules;
            _orderStatuses = orderStatuses;
            _retention = retention;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Let the host finish starting before the first database read.
            await Task.Yield();

            var slots = new SemaphoreSlim(Math.Clamp(_options.CurrentValue.WorkerConcurrency, 1, 32));
            var sweep = SweepLoopAsync(stoppingToken);

            try
            {
                await foreach (var runId in _queue.ReadAllAsync(stoppingToken))
                {
                    // This process may be one that only creates runs (RunWorker false): drop the
                    // hand-off and let the process that does the work pick it up from the database.
                    if (!_options.CurrentValue.RunWorker)
                        continue;

                    // Claimed in the map BEFORE it starts, so a quick run cannot remove its entry
                    // before it was added (which would block that run in this process for good).
                    // Handed over while it is still going here (it released itself to come back in a
                    // moment, an admin acted on it): it is handed over again once this pickup ends.
                    var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    if (!_inFlight.TryAdd(runId, done.Task))
                    {
                        _again.TryAdd(runId, 0);
                        continue;
                    }

                    try
                    {
                        await slots.WaitAsync(stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _inFlight.TryRemove(runId, out _);
                        done.TrySetResult();
                        throw;
                    }

                    _ = RunOneAsync(runId, slots, done, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutting down.
            }

            await sweep;

            // Give runs in progress a moment to reach a step boundary and record where they stopped.
            var running = _inFlight.Values.ToArray();
            if (running.Length > 0)
                await Task.WhenAny(Task.WhenAll(running), Task.Delay(TimeSpan.FromSeconds(20), CancellationToken.None));
        }

        private async Task RunOneAsync(long runId, SemaphoreSlim slots, TaskCompletionSource done, CancellationToken stoppingToken)
        {
            // Off the reading loop, so one slow run never holds up the others.
            await Task.Yield();
            try
            {
                await _runner.RunAsync(runId, _workerId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server flow run {RunId} stopped with an error.", runId);
            }
            finally
            {
                _inFlight.TryRemove(runId, out _);
                slots.Release();
                done.TrySetResult();
                if (_again.TryRemove(runId, out _) && !stoppingToken.IsCancellationRequested)
                    _queue.Enqueue(runId);
            }
        }

        private async Task SweepLoopAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // The sweeps read the database every round, so only the processes that do the
                    // work run them (RunWorker); the web servers that only create runs (a page, a
                    // webhook, a payment) leave them to those. The run key keeps every run single
                    // whichever process gets there first.
                    if (_options.CurrentValue.Enabled && _options.CurrentValue.RunWorker)
                    {
                        await QueueDueRunsAsync(stoppingToken);
                        // Old finished runs are cleared where the work is done (at most once an hour).
                        await _retention.SweepAsync(stoppingToken);
                        await _triggers.SweepMissedAsync(stoppingToken);
                        await _schedules.SweepAsync(stoppingToken);
                        await _orderStatuses.SweepAsync(stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    // A database hiccup must not end the worker: try again next round.
                    _logger.LogError(ex, "Server flow sweep failed.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(_options.CurrentValue.SweepSeconds, 5, 600)), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async Task QueueDueRunsAsync(CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            var now = DateTimeOffset.UtcNow;
            var due = await scope.ServiceProvider.GetRequiredService<IRepository<ServerFlowRun>>().Query().AsNoTracking()
                .Where(r => ((r.Status == ServerFlowRunStatus.Queued || r.Status == ServerFlowRunStatus.Waiting)
                             && (r.NextAttemptAt == null || r.NextAttemptAt <= now)
                             && (r.LeaseUntil == null || r.LeaseUntil < now))
                            || (r.Status == ServerFlowRunStatus.Running && r.LeaseUntil < now))
                .OrderBy(r => r.NextAttemptAt)
                .Select(r => r.Id)
                .Take(DueBatch)
                .ToListAsync(ct);

            foreach (var id in due.Where(id => !_inFlight.ContainsKey(id)))
                _queue.Enqueue(id);
        }

        private static string Truncate(string text, int max) => text.Length <= max ? text : text[..max];
    }
}
