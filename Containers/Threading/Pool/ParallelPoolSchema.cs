using Containers.Models;

namespace Containers.Threading.Pool
{
    internal class ParallelPoolSchema : ParallelSchema
    {

        public int MaxPoolSize { get; set; }

        public ParallelPoolSchema()
        {

        }

        public override void OnStart()
        {
            throw new NotImplementedException();
        }

        public override void RunModel(Model model)
        {
            throw new NotImplementedException();
        }
    }
}
