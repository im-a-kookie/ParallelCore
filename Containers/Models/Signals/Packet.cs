namespace Containers.Models.Signals
{

    /// <summary>
    /// A packet that does not contain explicitly typed data
    /// </summary>
    public class Packet
    {
        /// <summary>
        /// the data object
        /// </summary>
        private object? data;

        /// <summary>
        /// Creates an empty packet
        /// </summary>
        public Packet() { }

        /// <summary>
        /// Creates a packet with the given data object
        /// </summary>
        /// <param name="data"></param>
        public Packet(object? data)
        {
            this.data = data;
        }

        /// <summary>
        /// Gets the typed data from this packet, or returns null if
        /// the data does not match the requested type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>An object of type <typeparamref name="T"/>, or null if no such cast exists.</returns>
        public T? GetData<T>()
        {
            if (data is T t) return t;
            return default;
        }

        /// <summary>
        /// Gets the data object from this packet.
        /// </summary>
        /// <returns>Null if no object</returns>
        public object? GetData()
        {
            return data;
        }
    }

}
