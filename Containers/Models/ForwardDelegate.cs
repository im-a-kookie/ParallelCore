using Containers.Models.Abstractions;
using Containers.Models.Signals;

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
