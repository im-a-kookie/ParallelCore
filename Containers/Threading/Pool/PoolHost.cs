using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
