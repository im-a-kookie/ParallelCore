using Containers.Models.Attributes;
using System.Reflection;

namespace Containers.Models
{
    /// <summary>
    /// The explorer searches a type and constructs callback mappings for it.
    /// </summary>
    public class Explorer
    {

        public void ExploreType(Type t)
        {
            if (t.IsAssignableTo(typeof(Model)))
                throw new ArgumentException("The type being explored must be a subclass of Models.Model!");

            // Go through all of the methods
            // We want literally all of them
            foreach (var method in t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attribute = method.GetCustomAttribute<Endpoint>();
                if (attribute != null)
                {

                    // now let's generate the method









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

        }

    }
}
