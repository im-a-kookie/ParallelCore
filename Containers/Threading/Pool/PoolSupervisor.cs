using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Containers.Threading.Pool
{
    internal class PoolSupervisor : PoolWorker
    {
        public PoolSupervisor(ParallelPoolSchema provider) : base(provider)
        {
        }

        public override void _EntryPoint()
        {
            
        }
    }
}
