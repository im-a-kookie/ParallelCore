using Containers.Models;

namespace Containers.Threading.Pool
{
    public class ParallelPoolSchema : ParallelSchema
    {

        public PoolSupervisor? PoolSupervisor { get; private set; }

        public int MaxPoolSize { get; set; }

        public ParallelPoolSchema()
        {

            CancellationSource = new();
            CancellationToken = CancellationSource.Token;

        }

        internal override void NotifySchemaStarted()
        {
            //create a new pool supervisor
            PoolSupervisor = new PoolSupervisor(this);
        }

        internal override void ProvideModelToThreads(Model model)
        {
            PoolSupervisor?.AwaitingModels.Enqueue(model);
            PoolSupervisor?.signal.Set();
        }



    }
}
