using Containers.Signals;
using System.Collections.Concurrent;

namespace Containers.Models
{
    public class SignalQueue : ISignalQueue, IDisposable
    {
        private int _maxSize = -1;

        private BlockingCollection<Signal> _internalQueue = new();

        ReaderWriterLockSlim _lock = new();


        private long _soonestExpirationTime = DateTime.MaxValue.Ticks;

        public int Count()
        {
            return _internalQueue.Count;
        }

        public void Dispose()
        {
            _lock.Dispose();
        }

        public bool Lock(int timeoutMs = -1)
        {
            return _lock.TryEnterWriteLock(timeoutMs);
        }

        public void Unlock()
        {
            if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
        }

        public bool Queue(Signal signal, int timeoutMs = -1)
        {
            if (_maxSize > 0 && Count() >= _maxSize) return false;
            if (_lock.TryEnterUpgradeableReadLock(timeoutMs))
            {
                try
                {
                    //add the signal to the queue
                    _internalQueue.Add(signal);
                    // Atomically update the soonest expiration time if the new expiration time is earlier
                    long currentMin = _soonestExpirationTime;
                    long newMin = Math.Min(currentMin, signal.Expiration.Ticks);
                    // If currentMin is still the same, update it with newMin
                    Interlocked.CompareExchange(ref _soonestExpirationTime, newMin, currentMin);

                    CleanExpiredMessages();
                    return true;
                }
                finally
                {
                    if (_lock.IsUpgradeableReadLockHeld) _lock.ExitUpgradeableReadLock();
                }
            }
            return false;
        }

        public void SetMaxSize(int size)
        {
            _maxSize = size;
        }

        public bool TryGet(TimeSpan timeout, out Signal? result)
        {
            _lock.EnterReadLock();
            try
            {
                //add the signal to the queue
                return _internalQueue.TryTake(out result, timeout);
            }
            finally
            {
                if (_lock.IsReadLockHeld) _lock.ExitReadLock();
            }
        }


        public void CleanExpiredMessages()
        {
            var time = DateTime.UtcNow.Ticks;
            if (time > _soonestExpirationTime)
            {
                Lock(-1);
                try
                {
                    _soonestExpirationTime = DateTime.MaxValue.Ticks;
                    // Popping equal to the count will cycle the entire queue
                    int reps = _internalQueue.Count;
                    for (int i = 0; i < reps; ++i)
                    {
                        // so take it and only add it back if it's unexpired
                        if (_internalQueue.TryTake(out var item))
                        {
                            if (item.Expiration.Ticks > time)
                            {
                                _internalQueue.Add(item);
                                if(item.Expiration.Ticks < _soonestExpirationTime)
                                    _soonestExpirationTime = item.Expiration.Ticks;
                            }
                        }
                    }
                }
                finally
                {
                    Unlock();
                }
            }
        }
    }
}
