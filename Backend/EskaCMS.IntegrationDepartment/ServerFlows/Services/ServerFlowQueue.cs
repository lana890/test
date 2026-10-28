using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Services
{
    /// <summary>
    /// In-process hand-off of run ids to the worker, so a run starts the moment it is created.
    /// Only a shortcut: the database is the real queue, and the worker's sweep finds every due run
    /// whether or not it was ever put here (e.g. after a restart). A run is in the hand-off once at
    /// most, however often it is handed over before the worker takes it.
    /// </summary>
    public sealed class ServerFlowQueue : IDisposable
    {
        /// <summary>The most runs waiting for a short wait to pass at one time; past that the sweep picks them up.</summary>
        private const int MaxTimers = 1000;

        private readonly Channel<long> _channel = Channel.CreateBounded<long>(new BoundedChannelOptions(10_000)
        {
            FullMode = BoundedChannelFullMode.DropWrite, // full: the sweep picks it up
            SingleReader = true,
        });

        private readonly ConcurrentDictionary<long, byte> _waiting = new();
        private readonly CancellationTokenSource _closing = new();
        private int _timers;

        public void Enqueue(long runId)
        {
            if (!_waiting.TryAdd(runId, 0))
                return; // already waiting for the worker
            if (!_channel.Writer.TryWrite(runId))
                _waiting.TryRemove(runId, out _);
        }

        /// <summary>The runs handed over, in order, each as the worker takes it.</summary>
        public async IAsyncEnumerable<long> ReadAllAsync([EnumeratorCancellation] CancellationToken ct)
        {
            await foreach (var runId in _channel.Reader.ReadAllAsync(ct))
            {
                _waiting.TryRemove(runId, out _);
                yield return runId;
            }
        }

        /// <summary>
        /// Hands a run over again when it is due, for a wait shorter than the sweep (a quick retry, a
        /// polling attempt): it would otherwise wait for the next sweep. Longer waits, and any past
        /// <see cref="MaxTimers"/> at once, are left to the sweep; nothing waits on once the server stops.
        /// </summary>
        public void EnqueueAt(long runId, DateTimeOffset due, TimeSpan sweepEvery)
        {
            var wait = due - DateTimeOffset.UtcNow;
            if (wait >= sweepEvery || _closing.IsCancellationRequested)
                return;
            if (Interlocked.Increment(ref _timers) > MaxTimers)
            {
                Interlocked.Decrement(ref _timers);
                return;
            }
            _ = EnqueueAfterAsync(runId, wait < TimeSpan.Zero ? TimeSpan.Zero : wait);
        }

        private async Task EnqueueAfterAsync(long runId, TimeSpan wait)
        {
            try
            {
                // a moment after the due time, so the run is due when the worker claims it
                await Task.Delay(wait + TimeSpan.FromMilliseconds(100), _closing.Token);
                Enqueue(runId);
            }
            catch (OperationCanceledException)
            {
                // the server is stopping: the run is in the database and the next start picks it up
            }
            finally
            {
                Interlocked.Decrement(ref _timers);
            }
        }

        public void Dispose() => _closing.Cancel();
    }

    /// <summary>
    /// Wakes whoever waits for a subject (the payment return page waiting for its order) when one of
    /// its runs changes state. In-process only; waiters also re-read the database, so a run finished
    /// by another process is still seen.
    /// </summary>
    public sealed class ServerFlowSignals
    {
        private readonly ConcurrentDictionary<long, TaskCompletionSource> _waiting = new();

        /// <summary>A wake-up for one subject, listening from the moment it was armed. Dispose it when done.</summary>
        public sealed class Wake : IDisposable
        {
            private readonly ConcurrentDictionary<long, TaskCompletionSource> _owner;

            internal Wake(ConcurrentDictionary<long, TaskCompletionSource> owner, long subjectId, TaskCompletionSource tcs)
            {
                _owner = owner;
                SubjectId = subjectId;
                Tcs = tcs;
            }

            internal long SubjectId { get; }
            internal TaskCompletionSource Tcs { get; }

            /// <summary>Never leaves an entry behind for a subject nobody pulses.</summary>
            public void Dispose()
            {
                if (!Tcs.Task.IsCompleted)
                    _owner.TryRemove(new KeyValuePair<long, TaskCompletionSource>(SubjectId, Tcs));
            }
        }

        /// <summary>
        /// Starts listening for <paramref name="subjectId"/>. Arm it BEFORE reading the run's state:
        /// a change between that read and <see cref="WaitAsync(Wake, TimeSpan, CancellationToken)"/>
        /// then still wakes the waiter instead of being missed until the next database check.
        /// </summary>
        public Wake Arm(long subjectId) =>
            new(_waiting, subjectId, _waiting.GetOrAdd(subjectId, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)));

        /// <summary>
        /// Returns when the armed subject changed (also before this call) or <paramref name="max"/>
        /// passes. Another waiter holding the same wake-up after this one is disposed wakes on its
        /// own timeout (waiters wait a few seconds at a time).
        /// </summary>
        public async Task WaitAsync(Wake wake, TimeSpan max, CancellationToken ct)
        {
            try
            {
                await wake.Tcs.Task.WaitAsync(max, ct);
            }
            catch (TimeoutException)
            {
                // Nothing happened in time: the caller re-reads the database.
            }
        }

        public void Pulse(long subjectId)
        {
            if (_waiting.TryRemove(subjectId, out var tcs))
                tcs.TrySetResult();
        }
    }
}
