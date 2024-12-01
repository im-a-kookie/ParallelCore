using Containers.Addressing;
using Containers.Models;

namespace Containers.Threading
{
    public abstract class Container : Addressable, IDisposable
    {

        public Model Child;

        public Container(Model m) : base()
        {
            Child = m;
        }


        public abstract void Notify();

        public virtual void Dispose()
        {
            



        }
    }
}
