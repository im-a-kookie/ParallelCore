using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Containers.Threading.Pool
{
    abstract class PoolWorker
    {
        ParallelPoolSchema ParallelProvider;
        public PoolWorker(ParallelPoolSchema provider)
        {
            this.ParallelProvider = provider;
        }


        public abstract void _EntryPoint();

    }
}
