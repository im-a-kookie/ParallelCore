using System.Collections.Concurrent;
using System.Diagnostics;

namespace Containers.Threading.Pool
{
    internal class PoolHost : PoolWorker
    {

        /// <summary>
        /// A list of containers that are owned by this host. The host will never interact with this list,
        /// only the supervisor.
        /// </summary>
        public List<PoolContainer> Containers = new List<PoolContainer>();

        /// <summary>
        /// The internal scheduled container list
        /// </summary>
        public BlockingCollection<PoolContainer> ScheduledContainers = new();

        /// <summary>
        /// An internal flag indicating whether this worker should die
        /// </summary>
        internal bool ShouldDie { get; set; }

        /// <summary>
        /// An internal flag indicating whether this worker did die
        /// </summary>
        internal bool DidDie { get; private set; }

        /// <summary>
        /// A floating point value estimating the utilization fration of this thread,
        /// from 0 (no utilization) to 1 (full utililization).
        /// </summary>
        public double EstimatedLoad = 0;

        /// <summary>
        /// The supervisor that oversees this hist
        /// </summary>
        PoolSupervisor Supervisor;

        /// <summary>
        /// A signal that is used to control the thread loop of this host
        /// </summary>
        public AutoResetEvent signal = new AutoResetEvent(false);

        /// <summary>
        /// Creates and runs a new host for the given schema and supervisor
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="supervisor"></param>
        public PoolHost(ParallelPoolSchema provider, PoolSupervisor supervisor) : base(provider)
        {
            Supervisor = supervisor;
        }

        /// <summary>
        /// Queues the given container for an update
        /// </summary>
        /// <param name="c"></param>
        internal void QueueContainer(PoolContainer c)
        {
            // ensure we can exit out
            if (!ParallelProvider.ShouldRun) return;
            // Try not to queue it multiple times
            if (Interlocked.Increment(ref c.queuedState) > 1)
            {
                Interlocked.Decrement(ref c.queuedState);
                return;
            }

            // Now schedule it and we're good
            ScheduledContainers.TryAdd(c);
            signal.Set();
        }


        public override void _EntryPoint(CancellationToken cancellationToken)
        {
            try
            {
                Stopwatch timer = Stopwatch.StartNew();

                while (!ShouldDie && ParallelProvider.ShouldRun && !cancellationToken.IsCancellationRequested)
                {
                    //count the time in the loop
                    timer.Restart();
                    double _last = timer.ElapsedMilliseconds;
                    while (ScheduledContainers.TryTake(out var container, 0))
                    {
                        Interlocked.Decrement(ref container!.queuedState);

                        // Enter the model
                        try
                        {
                            container._AllocationLock.EnterReadLock();
                            container.Child?.OnModelEnter(cancellationToken);
                        }
                        catch(Exception ex)
                        {
                            Logger.Default.WriteBlock($"{ex} Exception, {ex.Message}", 
                                $"Thread:    0x{Address}\n" +
                                $"Container: 0x{container.Address}\n" +
                                $"Model:     0x{container.Child.Address}");
                        }
                        finally
                        {
                            if (container._AllocationLock.IsReadLockHeld)
                                container._AllocationLock.ExitReadLock();
                        }

                        // Check if we should try to queue again
                        if (container?.Child?.ExpectedIterationsPerSecond > 0)
                        {
                            container?.Schedule(
                                ms_delay: (int)(1000 / (container?.Child?.ExpectedIterationsPerSecond ?? 1)));
                        }
                        
                        // calculate the elapsed amount
                        double elapsed = timer.ElapsedMilliseconds - _last;
                        // TODO pass elapsed time back into container so that we can measure the utilization correctly
                    }
                    double runTime = timer.ElapsedMilliseconds;

                    signal.WaitOne(-1);

                    double loopTime = timer.ElapsedMilliseconds;

                    // we can now calculate an approximate load rate
                    // with a small epsilon to improve stability
                    double load = (runTime + 1e-9) / (loopTime + 1e-9);

                    // We can now accumulate this as a running average into the public field
                    // First let's estimate a numerical value
                    double period = 5000; // window of measurement in milliseconds
                    double normal = 50; // a stability factor
                    double iterations = period / (loopTime + normal); //the total iterations

                    // Now we can estiamte the mean as a total based on the mean + iterations
                    double estimated_total = EstimatedLoad * iterations;
                    double new_total = estimated_total + loopTime; // add the new delay
                    
                    // and recalculate the average
                    EstimatedLoad = new_total / (iterations + 1);


                }

            }
            finally
            {
                // Mark that this thread is over. The supervisor will clean it up.
                DidDie = true;
                Interlocked.Decrement(ref Supervisor.TotalThreads);
                Supervisor.signal.Set();
            }

        }
    }

}
