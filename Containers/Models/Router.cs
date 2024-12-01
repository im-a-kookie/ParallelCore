using Containers.Addressing;
using Containers.Models.Signals;
using Containers.Signals;

namespace Containers.Models
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
        /// The ID address of this router (unique)
        /// </summary>
        private Address<int> ID;

        /// <summary>
        /// The ID address of the model category that binds to this router
        /// </summary>
        private Type ModelType;

        /// <summary>
        /// The signal dictionary that translates comings and goings from this router
        /// </summary>
        public SignalDictionary SignalDictionary { get; set; }

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
            ID = id;
            ModelType = modelType;
            SignalDictionary = new SignalDictionary(modelType);
        }

        /// <summary>
        /// Gets the wrapper indicated by the given header
        /// </summary>
        /// <param name="header"></param>
        /// <returns></returns>
        public Wrapper? GetCallback(Header header)
        {
            return SignalDictionary.GetWrapper(header);
        }

    }
}
