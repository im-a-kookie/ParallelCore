using Containers.Addressing;
using Containers.Models;

namespace Containers.Signals
{
    /// <summary>
    /// The registry provides a simplfied translation between Header values and human-readable strings.
    /// 
    /// <para>
    /// All strings are mapped to an integer value by this class.
    /// </para>
    /// </summary>
    public class Router
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
        public delegate object? EndpointCallback(Signal? signal, object? data, Model? receiver, Provider? provider, ModelRegistry? registry, Router? router, string? command);

        /// <summary>
        /// The ID address of this router (unique)
        /// </summary>
        Address<int> ID;

        /// <summary>
        /// The ID address of the model category that binds to this router
        /// </summary>
        Type ModelType;


        /// <summary>
        /// A class that provides signal-lookups for the provided model
        /// </summary>
        SignalDictionary Lookup = new();


        /// <summary>
        /// Creates a new Router that with the provided ID, that binds to the given model type (that must inherit <see cref="Model"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="modelType"></param>
        /// <exception cref="ArgumentException"></exception>
        public Router(Address<int> id, Type modelType)
        {
            // Validate the class
            if (!ModelRegistry.ValidateModelType(modelType))
            {
                throw new ArgumentException($"Cannot register {modelType} as a Model. The class must inherit the Model base class!");
            }

            // Set the fields
            this.ID = id;
            this.ModelType = modelType;
        }



    }
}
