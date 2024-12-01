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

        public override void OnStart()
        {
            //create a new pool supervisor
            PoolSupervisor = new PoolSupervisor(this);
        }

        public override void RunModel(Model model)
        {
            PoolSupervisor?.AwaitingModels.Enqueue(model);
            PoolSupervisor?.signal.Set();
        }



    }
}
