using Containers.Signals;
using System.Collections.Concurrent;

namespace Containers.Models
{
    internal class SignalQueue : ISignalQueue, IDisposable
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

        public bool Queue(Signal signal)
        {
            if (_maxSize > 0 && Count() > _maxSize) return false;
            _lock.EnterReadLock();
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

        public void SetMaxSize(int size)
        {
            _maxSize = size;
        }

        public bool TryGet(int timeout, out Signal? result)
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

        public bool TryGet(out Signal? result)
        {
            _lock.EnterReadLock();
            try
            {
                //add the signal to the queue
                return _internalQueue.TryTake(out result);
            }
            finally
            {
                if (_lock.IsReadLockHeld) _lock.ExitReadLock();
            }
        }


    }
}
