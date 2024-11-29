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
