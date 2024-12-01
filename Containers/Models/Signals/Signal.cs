namespace Containers.Models.Signals
{

    /// <summary>
    /// The Signal class represents a message stored in the message queue.
    /// 
    /// <para>
    /// Boxing for future serialization.
    /// </para>
    /// </summary>
    public class Signal
    {
        public Header Flag;

        public Model? Receiver;

        public Packet? Data;

        public TaskCompletionSource<object?>? CompletionSource;

        public DateTime Expiration = DateTime.MaxValue;

    }
}
