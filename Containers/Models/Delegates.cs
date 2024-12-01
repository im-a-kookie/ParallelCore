using Containers.Signals;
using Containers.Threading;

namespace Containers.Models
{
    public class Delegates
    {
        /// <summary>
        /// The main delegate callback that is used to provide to endpoints.
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="data"></param>
        /// <param name="receiver"></param>
        /// <param name="provider"></param>
        /// <param name="registry"></param>
        /// <param name="router"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        /// <remarks>
        /// Implementation assumes dynamic remapping of parameters. Declaration order of parameters indicates priority when wildcarding.
        /// </remarks>
        public delegate object? EndpointCallback(object? data, Model? receiver, ParallelSchema? provider, ModelRegistry? registry, Router? router);

        /// <summary>
        /// Delegate allowing a signal callback to be invoked on the model
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public delegate object? SignalDelegate(object? data = null);

        /// <summary>
        /// Delegate allowing a signal callback to be invoked on the model
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public delegate object? SignalDelegate<T>(T? data = default);


    }
}
