using Containers.Models;
using System.Diagnostics.Metrics;

namespace Containers.Threading.Pool
{
    public class PoolContainer : Container
    {
        /// <summary>
        /// The average number of milliseconds taken to execute this container, estimated
        /// over the past few moments with a rolling average calculation.
        /// </summary>
        public double performance_metric = 0;

        /// <summary>
        /// A boolean flag indicating whether this container should exit
        /// </summary>
        public bool ShouldDie { get; private set; }

        /// <summary>
        /// An indicator used to hold this container during threadpool task balancing
        /// operations.
        /// </summary>
        public ReaderWriterLockSlim _AllocationLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Notifies that this container should die
        /// </summary>
        public void Kill()
        {
            ShouldDie = true;
            host?.signal.Set();
        }

        /// <summary>
        /// A counter indicating whether this container is currently queued to be processed
        /// </summary>
        internal int queuedState = 0;

        /// <summary>
        /// A counter indicating whether this container is currently awaiting a schedule event
        /// </summary>
        internal int awaitingState = 0;

        /// <summary>
        /// The host that currently owns this container
        /// </summary>
        internal PoolHost? host;

        public PoolContainer(Model m) : base(m)
        {
        }

        /// <summary>
        /// Notifies the host that this container is ready for an update
        /// </summary>
        public override void Notify()
        {
            try
            {
                // Ensure that we can notify and so on
                _AllocationLock.EnterReadLock();
                if (host?.ParallelProvider.ShouldRun ?? false)
                {
                    host?.QueueContainer(this);
                }
            }
            finally
            {
                if(_AllocationLock.IsReadLockHeld)
                {
                    _AllocationLock.ExitReadLock();
                }
            }

        }
        
        /// <summary>
        /// Schedules this container to be run again after the given number of milliseconds
        /// </summary>
        /// <param name="ms_delay"></param>
        public void Schedule(int ms_delay)
        {            
            //add us to the thing
            int counter = Interlocked.Increment(ref awaitingState);
            if (counter > 1)
            {
                Interlocked.Decrement(ref awaitingState);
                return;
            }

            Task.Run(() =>
            {
                var t = host?.ParallelProvider.CancellationToken;
                if(t != null && !t.Value.IsCancellationRequested)
                {
                    Task.Delay(ms_delay, t.Value);
                }
                else
                {
                    return;
                }
                // now schedule it
                Notify();
                Interlocked.Decrement(ref awaitingState);
            });
        }


        /// <summary>
        /// Dispose this container and release the locks.
        /// </summary>
        public override void Dispose()
        {
            _AllocationLock.Dispose();
        }
}
    }
