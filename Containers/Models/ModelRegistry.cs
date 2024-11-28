using Containers.Addressing;
using Containers.Models;
using Containers.Models.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
        /// The global model address provider, which provides unique addresses
        /// across the entire instance provider
        /// </summary>
        IAddressProvider<long> GlobalModelAddressProvider = new AddressProvider<long>();

        /// <summary>
        /// The address provider which provides ID values for each Type that can be instantiated as a model
        /// </summary>
        IAddressProvider<int> TypeAddressProvider = new AddressProvider<int>();

        /// <summary>
        /// A dictionary that maps the ID address to the type
        /// </summary>
        ConcurrentDictionary<Address<int>, Type> _idToType = [];

        /// <summary>
        /// A dictionary mapping of type to Address iD
        /// </summary>
        ConcurrentDictionary<Type, Address<int>> _typeToId = [];

        /// <summary>
        /// A dictionary that maps the ID address to the signal router
        /// </summary>
        ConcurrentDictionary<Address<int>, Router> _idToRouter = [];

        /// <summary>
        /// A dictionary mapping of type to the signal router
        /// </summary>
        ConcurrentDictionary<Type, Router> _typeToRouter = [];

        /// <summary>
        /// Register a given type to be instantiated as a model. This method should be Idempotent for like types.
        /// </summary>
        public void Register(Type t)
        {
            lock (this)
            {
                //create a new model container
                if (!ValidateModelType(t))
                    throw new ArgumentException($"Cannot register {t} as a Model. The class must inherit the Model base class!");

                //now add the things
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

            if(_typeToRouter.TryGetValue(t, out var router))
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
