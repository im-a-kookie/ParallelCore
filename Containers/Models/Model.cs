using Containers.Addressing;
using Containers.Models.Attributes;
using Containers.Signals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Containers.Models
{
    [ModelDefinition("Default_Model")]
    public class Model
    {

        /// <summary>
        /// The signal registry that provides signals for the input systems in this model
        /// </summary>
        public Router SignalRegistry { get; private set; }



        public Model(Provider provider)
        {

        }


    }
}
