using Containers.Models.Signals;

namespace Containers.Models
{
    public abstract class Wrapper
    {
        /// <summary>
        /// The index of this wrapper in the underlying signal dictionary 
        /// </summary>
        public int index = -1;

        /// <summary>
        /// The internal names/aliases for this wrapper
        /// </summary>
        public List<string> Names = [];

        /// <summary>
        /// Invoke this wrapper to get a generic object
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public abstract object? Invoke(Signal signal);

        /// <summary>
        /// Invokes this wrapper to get a return of type T, or null otherwise
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="receiver"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public abstract T? Invoke<T>(Signal signal) where T : notnull;


    }

    /// <summary>
    /// A typed wrapper dictating the expected Data and Return types for simplicity.
    /// 
    /// <para>In theory this can be merged into the router or the model, but certain
    /// typecasting aspects are annoying in the delegate builder, so this fixes it</para>
    /// </summary>
    /// <typeparam name="Data"></typeparam>
    /// <typeparam name="Return"></typeparam>
    public class Wrapper<Data, Return> : Wrapper
    {

        /// <summary>
        /// The callback delegate provided by this wrapper
        /// </summary>
        public Delegates.EndpointCallback Callback;

        /// <summary>
        /// Creates a new wrapper for the given endpoint
        /// </summary>
        /// <param name="callback"></param>
        public Wrapper(Delegates.EndpointCallback callback)
        {
            Callback = callback;
        }

        /// <summary>
        /// Calls this wrapper, passing the call back through the given delegate.
        /// </summary>
        /// <param name="signal"></param>
        /// <returns></returns>
        public override object? Invoke(Signal signal)
        {
            // Build out the parameters
            var data = signal.Data?.GetData();
            var receiver = signal.Receiver;
            if (data is Data d)
            {
                return Callback(d, receiver, receiver?.Parent, receiver?.Parent?.ModelRegistry, receiver?.SignalRegistry);
            }
            else
            {
                return Callback(default(Data), receiver, receiver?.Parent, receiver?.Parent?.ModelRegistry, receiver?.SignalRegistry);
            }
        }

        /// <summary>
        /// Invokes this wrapper with the expected return type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="signal"></param>
        /// <returns>An instance of the requested return type, or default(T) if no suitable type is returnable.</returns>
        public override T? Invoke<T>(Signal signal) where T : default
        {
            var result = Invoke(signal);
            if (result is T t) return t;
            return default;
        }

        /// <summary>
        /// Gets the header for this wrapper
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Header GetHeader()
        {
            if (index < 0)
            {
                throw new InvalidOperationException(
                    $"Error creating header. The index is not initialized. Expect: 0<index<{Header.Max_Value}, Index: {index}");

            }

            if (index > Header.Max_Value)
            {
                throw new InvalidOperationException(
                    $"Error creating header. The max value has been exceeded. Expect: 0<index<{Header.Max_Value}, Index: {index}");
            }
            return new Header((ushort)index);
        }


    }
}
