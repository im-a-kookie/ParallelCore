using Containers.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
