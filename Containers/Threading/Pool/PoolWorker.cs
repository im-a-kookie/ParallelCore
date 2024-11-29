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
