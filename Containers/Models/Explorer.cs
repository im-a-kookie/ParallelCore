using Containers.Emission;
using Containers.Models.Attributes;
using Containers.Signals;
using System.Reflection;

namespace Containers.Models
{
    /// <summary>
    /// The explorer searches a type and constructs callback mappings for it.
    /// </summary>
    public class Explorer
    {

        public static List<Wrapper> ExploreType(Type t)
        {
            if (t.IsAssignableTo(typeof(Model)))
                throw new ArgumentException("The type being explored must be a subclass of Models.Model!");

            List<Wrapper> results = new();

            // Go through all of the methods
            // We want literally all of them
            foreach (var method in t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attribute = method.GetCustomAttribute<Endpoint>();
                if (attribute != null)
                {

                    // now let's generate the method

                    var result = DelegateBuilder.CreateCallbackDelegate(method, out var context);

                    // We need to generate the generic wrapper type, for reasons
                    var wrapperType = typeof(Wrapper<,>).MakeGenericType(context!.TargetReturn, context.DataType);
                    var wrapperConstructor = wrapperType?.GetConstructor([typeof(Router.EndpointCallback)]);
                    var wrapper = wrapperConstructor?.Invoke([result]);

                    // Now, we can install it
                    if(wrapper != null)
                    {
                        results.Add((Wrapper)wrapper);
                    }

                    // Alias tries to register first, then Name as default
                    string?[] names = [attribute.Alias, method.Name];
                    // But in some cases we want to add both
                    var nameCount = attribute.UseMethodName ? 2 : 1;

                    foreach (var name in names.Where(n => n != null))
                    {
                        // we've already added the alias so skip the method name
                        if (nameCount == 0) break;
                        --nameCount;
                    }
                }
            }

            return results;

        }

    }
}
