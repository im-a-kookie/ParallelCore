using Containers.Signals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Containers.Models
{
    internal class ForwardDelegate : IForwardDelegate
    {

        public Header Header { get; private set; }

        public Model Model { get; private set; }

        public ForwardDelegate(Model m, Header h)
        {
            this.Header = h;
            this.Model = m;
        }

        public Model GetCaller() => Model;

        public Header GetEndpoint() => Header;
    }
}
