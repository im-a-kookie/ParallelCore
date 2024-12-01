using Containers.Addressing;
using Containers.Models;
using Containers.Models.Attributes;
using System.Collections.Concurrent;
using System.Reflection;

namespace Containers.Signals
{
    /// <summary>
    /// A mapping that connects models to the model registry value.
    /// 
    /// <para>To be used, the type of a model must be registered in the model registry,
    /// and the type is assigned a unique ID value</para>
    /// </summary>
    public class ModelRegistry
    {
        /// <summary>
        /// Validates that the given type is a model type.
        /// </summary>
        /// <param name="t"></param>
        public static bool ValidateModelType(Type t)
        {
            // Ensure it's a model type and that it has the model definition attribute
            if (!t.IsAssignableTo(typeof(Model))) return false;
            var m = t.GetCustomAttribute(typeof(ModelDefinition));
            if (m == null) return false;
            return true;
        }

        /// <summary>
        /// A mapping of all active instances by their addressed ID
        /// </summary>
        private static ConcurrentDictionary<Address<long>, Model> _instanceMapping = [];

        /// <summary>
        /// Loads the given model into the global registry.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>This method is static, as it allows models to be found instance-wide between providers</remarks>
        public static bool LoadModel(Model model)
        {
            if (model.Constructed) return false;

            var address = model.Address;
            if (!_instanceMapping.TryAdd(address, model))
            {
                throw new InvalidOperationException($"A model is already registered to {address}!");
            }
            return true;
        }

        /// <summary>
        /// The address provider which provides ID values for each Type that can be instantiated as a model
        /// </summary>
        private IAddressProvider<int> TypeAddressProvider = new AddressProvider<int>();

        /// <summary>
        /// A dictionary that maps the ID address to the type
        /// </summary>
        private ConcurrentDictionary<Address<int>, Type> _idToType = [];

        /// <summary>
        /// A dictionary mapping of type to Address iD
        /// </summary>
        private ConcurrentDictionary<Type, Address<int>> _typeToId = [];

        /// <summary>
        /// A dictionary that maps the ID address to the signal router
        /// </summary>
        private ConcurrentDictionary<Address<int>, Router> _idToRouter = [];

        /// <summary>
        /// A dictionary mapping of type to the signal router
        /// </summary>
        private ConcurrentDictionary<Type, Router> _typeToRouter = [];

        /// <summary>
        /// Register a given type to be instantiated as a model. This method should be idempotent for like types.
        /// </summary>
        public void Register(Type t)
        {
            // We can break out easily before locking
            if (_typeToId.ContainsKey(t)) return;

            lock (this)
            {
                //create a new model container
                if (!ValidateModelType(t))
                    throw new ArgumentException($"Cannot register {t} as a Model. The class must inherit the Model base class!");

                // Double check locking pattern
                if (!_typeToId.ContainsKey(t))
                {
                    // Use a new address
                    var id = TypeAddressProvider.Get();
                    if (_typeToId.TryAdd(t, id) && _idToType.TryAdd(id, t))
                    {
                        // Make and register the router
                        Router router = new Router(id, t);
                        _typeToRouter.TryAdd(t, router);
                        _idToRouter.TryAdd(id, router);

                        // Go through all of the methods
                        // We want literally all of them
                        foreach (var method in t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            var attribute = method.GetCustomAttribute<Endpoint>();
                            if (attribute != null)
                            {
                                router.SignalDictionary.Register(method);
                            }
                        }

                        // doink
                        Logger.Default.Info($"Registered Model: {t}");
                    }
                }

            }
        }

        /// <summary>
        /// Gets the router for the given model. The model type must be registered first.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Router GetRouterForModel(Model model)
        {
            var t = model.GetType();

            if (_typeToRouter.TryGetValue(t, out var router))
            {
                return router!;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Model type {t.Name} Must be registered before use!");
            }
        }




    }
}
