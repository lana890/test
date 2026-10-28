namespace EskaCMS.IntegrationDepartment.ServerFlows.Services
{
    /// <summary>
    /// At most N per key and minute, counted in this process (a limit against floods, not an exact
    /// quota). Ended minutes are forgotten once a minute. The table holds at most a set number of
    /// keys (50 000): when a flood of new keys fills it, the tenth whose minute began longest ago
    /// makes room, so new callers are never all refused. One lock keeps the counts exact.
    /// </summary>
    public sealed class PerMinuteLimit
    {
        private static readonly TimeSpan Minute = TimeSpan.FromMinutes(1);

        private readonly int _maxKeys;

        public PerMinuteLimit(int maxKeys = 50_000) => _maxKeys = Math.Max(10, maxKeys);

        /// <summary>How many keys are counted now.</summary>
        internal int Keys
        {
            get
            {
                lock (_gate)
                    return _windows.Count;
            }
        }

        private sealed class Window
        {
            public DateTimeOffset Start;
            public int Count;
        }

        private readonly Dictionary<string, Window> _windows = new(StringComparer.Ordinal);
        private readonly object _gate = new();
        private DateTimeOffset _lastClean = DateTimeOffset.MinValue;

        public bool Take(string key, int perMinute)
        {
            var now = DateTimeOffset.UtcNow;
            lock (_gate)
            {
                if (now - _lastClean >= Minute)
                {
                    foreach (var ended in _windows.Where(w => now - w.Value.Start >= Minute).Select(w => w.Key).ToList())
                        _windows.Remove(ended);
                    _lastClean = now;
                }

                if (!_windows.TryGetValue(key, out var window))
                {
                    if (_windows.Count >= _maxKeys)
                        foreach (var oldest in _windows.OrderBy(w => w.Value.Start).Take(_maxKeys / 10).Select(w => w.Key).ToList())
                            _windows.Remove(oldest);
                    _windows[key] = new Window { Start = now, Count = 1 };
                    return true;
                }
                if (now - window.Start >= Minute)
                {
                    (window.Start, window.Count) = (now, 1);
                    return true;
                }
                if (window.Count >= Math.Max(1, perMinute))
                    return false;
                window.Count++;
                return true;
            }
        }
    }
}
