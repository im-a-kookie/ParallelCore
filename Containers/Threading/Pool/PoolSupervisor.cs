using Containers.Models;
using System.Collections.Concurrent;

namespace Containers.Threading.Pool
{
    public class PoolSupervisor : PoolWorker, IDisposable
    {

        public delegate double LoadFunction(int threads, double averageLoad);


        /// <summary>
        /// The total number of running threads
        /// </summary>
        public int TotalThreads = 0;

        /// <summary>
        /// The total number of threads that we want to run
        /// </summary>
        public int TargetThreads = 1;

        /// <summary>
        /// The maximum permissable number of threads
        /// </summary>
        public int MaxThreads = 8;

        /// <summary>
        /// The utilization priority function is used to transform the utilization value into a priority
        /// from 0 to 1, where 0 represents underutilization, and 1 represents overutilization.
        /// 
        /// <para>
        /// The load balancing algorithm seeks to optimize load balance such that this function produces a value
        /// of 0.5, while keeping the total thread count between 1 and <see cref="MaxThreads"/>.
        /// </para>
        /// 
        /// <para>
        /// Therefore, values returned above 0.5 will suggest to the threadpool to generate new threads,
        /// while values below 0.5 will suggest to close threads.</para>
        /// </summary>
        public LoadFunction UtilizationPriorityFunction = DefaultLoadFunction;

        /// <summary>
        /// A default load function that seeks to use an average utilization of 80% as the utilization target
        /// </summary>
        /// <param name="threads"></param>
        /// <param name="averageLoad"></param>
        /// <returns></returns>
        private static double DefaultLoadFunction(int threads, double averageLoad)
        {
            // A reasonable target load falls at around 0.8
            // so let's adjust this to 0.5
            // But use logarithms, so that the curve remains continuous
            double targetLoad = 0.8;
            double n = Math.Log(targetLoad);
            double x = Math.Log(0.5);

            return Math.Pow(averageLoad, x / n);
        }


        public ConcurrentQueue<Model> AwaitingModels = [];

        public PoolSupervisor(ParallelPoolSchema provider) : base(provider)
        {
        }

        /// <summary>
        /// A collection of all active threads
        /// </summary>
        private List<PoolHost> _activeThreads = [];

        public AutoResetEvent signal = new AutoResetEvent(false);

        public override void _EntryPoint(CancellationToken cancellationToken)
        {
            while (ParallelProvider.ShouldRun && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // we can do this safely, since it's just an ST counter
                    // and a single provider can handle everything
                    _activeThreads.RemoveAll(x => x.DidDie);

                    // Get all of the load stats across the board
                    List<double> load_stats = _activeThreads
                        .Where(x => !x.DidDie)
                        .Select(x => x.EstimatedLoad)
                        .ToList();

                    // Now let's calculate the best load
                    double currentTotal = load_stats.Sum();
                    double currentAverage = currentTotal / TotalThreads;

                    if (TotalThreads > 1)
                    {
                        double minValue = load_stats.Min();
                        double newTotal = currentTotal - minValue;
                        // now calculate the new average value
                        double newAvg = newTotal / (TotalThreads - 1);

                        // If the new total is below the minimum
                        // Then we can spin down the thread

                        // Here we will indicate to the thread that it should end,
                        // Then we will nudge down the thread target
                        // Causing it to quitly exit and for no new thread to start

                    }

                    // the currentAverage is above the goal
                    // then we can do a rough estimate of new distribution
                    // such that the distribution is a little fairer
                    // The logic is
                    // 1. If the utilization is high then we create a new thread


                    // Next, we perform a load balancing step
                    // Wherein we go through every host thread and every task they own
                    // For each of these against the host thread that is the furthest from the mean
                    // and we move the best fitting task to that thread

                    // Ensure there are always threads
                    while (TotalThreads < 1)
                    {
                        var counter = Interlocked.Increment(ref TotalThreads);
                        if (counter <= TargetThreads)
                        {
                            _activeThreads.Add(new PoolHost(ParallelProvider, this));
                        }
                        else
                        {
                            Interlocked.Decrement(ref TotalThreads);
                        }
                    }

                    // Queue up any models that are awaiting being started
                    while (AwaitingModels.TryDequeue(out var model))
                    {
                        //find the best host
                        double minLoad = double.MaxValue;
                        PoolHost? bestHost = _activeThreads.Where(x => !x.DidDie).FirstOrDefault();
                        foreach (var host in _activeThreads)
                        {
                            if (host.EstimatedLoad < minLoad)
                            {
                                minLoad = host.EstimatedLoad;
                                bestHost = host;
                            }
                        }
                        // Now create the container and point it at the best host thing
                        if (bestHost != null && !bestHost.DidDie && !bestHost.ShouldDie)
                        {
                            PoolContainer pc = new PoolContainer(model!);
                            model.NotifyContainerReceivedModel(pc);
                            pc.host = bestHost;
                            pc.Notify();
                        }
                    }

                    // Now clean up dead containers
                    foreach (var host in _activeThreads)
                    {
                        foreach (var container in host.Containers)
                        {
                            if (container.HasDied)
                            {
                                container.Child.InvokeModelDispose();
                                if (container.Child is IDisposable disposabe) disposabe.Dispose();
                                container.Dispose();
                            }
                        }


                    }



                    signal.WaitOne();
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(ex.Message);
                    Console.WriteLine($"Supervisor 0x{Address} Error: {ex.Message}, {ex.TargetSite}");

                }
            }

            Console.WriteLine("Bonked?");

        }

        public void Dispose()
        {
            signal.Dispose();
        }
    }
}
