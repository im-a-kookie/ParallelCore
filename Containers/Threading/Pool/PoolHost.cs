using System.Collections.Concurrent;

namespace Containers.Threading.Pool
{
    internal class PoolHost : PoolWorker
    {

        /// <summary>
        /// The internal scheduled container list
        /// </summary>
        public BlockingCollection<PoolContainer> ScheduledContainers = new();

        public PoolHost(ParallelPoolSchema provider) : base(provider)
        {
        }

        public override void _EntryPoint()
        {
            throw new NotImplementedException();
        }
    }

}
