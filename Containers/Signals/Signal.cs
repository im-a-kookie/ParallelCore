using Containers.Models;

namespace Containers.Signals
{
    public class Signal
    {
        public Header Flag;

        public Model? Sender;

        public Model? Receiver;

        public Packet? Data;

        public TaskCompletionSource<object?>? CompletionSource;

        public DateTime Expiration = DateTime.MaxValue;

    }
}
