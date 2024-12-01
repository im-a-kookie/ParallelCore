using Containers.Emission;
using Containers.Models.Attributes;
using Containers.Models.Signals;
using System.Reflection;

namespace Containers.Models
{
    public class SignalDictionary
    {
        private Lock _lock = new();

        private Type _owningModel;

        /// <summary>
        /// A list mapping of _wrappers to their header indices
        /// </summary>
        private List<Wrapper> _wrappers = [];

        /// <summary>
        /// An ordered collection of names/aliases
        /// </summary>
        private List<string[]> _names = [];

        /// <summary>
        /// A mapping of string command names to their index in <see cref="_wrappers"/> and <see cref="_names"/>.
        /// </summary>
        private Dictionary<string, int> _nameToIndex = [];


        public SignalDictionary(Type t)
        {
            if (t.IsAssignableTo(typeof(Model)))
            {
                _owningModel = t;
            }
            else throw new ArgumentException($"The type {t} must be a model!");
        }

        public SignalDictionary(Model m)
        {
            _owningModel = m.GetType();
        }

        public void Register(MethodInfo target)
        {


            var attribute = target.GetCustomAttribute<Endpoint>();
            if (attribute != null)
            {
                // Alias tries to register first, then Name as default
                string?[] names = [attribute.Alias, target.Name];
                // But in some cases we want to add both
                var nameCount = attribute.UseMethodName ? 2 : 1;

                // Generate the callback
                var callback = DelegateBuilder.CreateCallbackDelegate(target, out var context);

                // Extract usable types for the wrapper constructor
                var dataType = context?.DataType ?? typeof(object);
                if (dataType == typeof(void)) dataType = typeof(object);

                var returnType = context?.TargetReturn ?? typeof(object);
                if (returnType == typeof(void)) returnType = typeof(object);

                // Now make the generic
                var wrapper = typeof(Wrapper<,>).MakeGenericType(dataType, returnType);
                var result = wrapper.GetConstructor([typeof(Delegates.EndpointCallback)])!.Invoke([callback]);

                // Check that the result is good and then register
                if (result != null)
                {
                    int count = _wrappers.Count;
                    _wrappers.Add((Wrapper)result);

                    names = names.Where(x => x != null).ToArray();
                    nameCount = int.Min(names.Length, nameCount);
                    string[] newNames = new string[nameCount];
                    Array.Copy(names, newNames, nameCount);

                    _names.Add(newNames);
                    foreach (string s in newNames)
                    {
                        _nameToIndex.TryAdd(s, count);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the header from the given string command
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public Header? GetHeader(string command)
        {
            if (_nameToIndex.TryGetValue(command, out var index))
            {
                if (index < 0 || index >= _wrappers.Count) return null;
                return new Header((ushort)index);
            }
            return null;
        }

        /// <summary>
        /// Gets the wrapper for the given header
        /// </summary>
        /// <param name="h"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public Wrapper GetWrapper(Header h)
        {
            int n = h.ID;
            if (n < 0 || n >= _wrappers.Count)
                throw new IndexOutOfRangeException($"The header {h.ID} is not valid!");
            return _wrappers[n];
        }

        /// <summary>
        /// Gets the wrapper for the given string named endpoint
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public Wrapper GetWrapper(string name)
        {
            if (_nameToIndex.TryGetValue(name, out var index))
            {
                if (index >= 0 && index < _wrappers.Count)
                {
                    return _wrappers[index];
                }
            }
            throw new KeyNotFoundException($"A message by the name '{name}' does not exist!");
        }


    }
}
