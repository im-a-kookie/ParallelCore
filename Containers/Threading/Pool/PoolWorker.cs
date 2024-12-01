using Containers.Addressing;

namespace Containers.Threading.Pool
{
    public abstract class PoolWorker : Addressable
    {
        public ParallelPoolSchema ParallelProvider;
        public PoolWorker(ParallelPoolSchema provider) : base()
        {
            this.ParallelProvider = provider;
            // Start the thread
            Thread t = new Thread(() =>
            {
                _EntryPoint(ParallelProvider.CancellationToken);
            });
            // doink
            t.Start();
        }

        /// <summary>
        /// The Entry Point for the thread that runs within this pool worker entity
        /// </summary>
        /// <param name="cancellationToken"></param>
        public abstract void _EntryPoint(CancellationToken cancellationToken);

    }
}
