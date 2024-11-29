using Containers.Models.Attributes;
using Containers.Signals;

namespace Containers.Models
{
    [ModelDefinition("Default_Model")]
    public class Model
    {
        private bool _active = false;
        public bool IsActive => _active;

        /// <summary>
        /// The signal registry that provides signals for the input systems in this model
        /// </summary>
        public Router? SignalRegistry { get; internal set; }

        public ISignalQueue? MessageQueue { get; internal set; }

        public Model(Provider? provider)
        {
        }


        public void NotifyChanges() { }
        public void NotifyReceiveMessage() { }

        internal bool OnReceiveMessage(Signal signal)
        {
            return true;
        }




    }
}
