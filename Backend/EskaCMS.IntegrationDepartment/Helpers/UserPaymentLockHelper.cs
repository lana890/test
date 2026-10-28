using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Integrations.Helpers
{
	public class UserPaymentLockHelper
	{
		private static readonly ConcurrentDictionary<long, SemaphoreSlim> _locks
		= new();

		public static SemaphoreSlim GetLock(long userId)
		{
			return _locks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
		}

		public static void ReleaseLock(long userId)
		{
			if (_locks.TryGetValue(userId, out var semaphore))
			{
				if (semaphore.CurrentCount == 1)
					_locks.TryRemove(userId, out _);
			}
		}
	}
}
