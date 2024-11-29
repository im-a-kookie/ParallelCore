using Containers.Signals;
using System.Collections.Concurrent;

namespace Containers.Models
{
    public class SignalQueue : ISignalQueue, IDisposable
    {
        private int _maxSize = -1;

        private BlockingCollection<Signal> _internalQueue = new();

        ReaderWriterLockSlim _lock = new();

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
            if (_lock.TryEnterReadLock(timeoutMs))
            {
                try
                {
                    //add the signal to the queue
                    _internalQueue.Add(signal);
                    return true;
                }
                finally
                {
                    if (_lock.IsReadLockHeld) _lock.ExitReadLock();
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


    }
}
