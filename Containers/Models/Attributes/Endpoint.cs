using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Containers.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class Endpoint : Attribute
    {
        /// <summary>
        /// The alias under which this endpoint will be registered
        /// </summary>
        public string? Alias { get; private set; }
        /// <summary>
        /// Whether to register using the method name when an alias is provided (in which case BOTH will work).
        /// </summary>
        public bool UseMethodName {  get; private set; }
        public Endpoint()
        {
        }

        /// <summary>
        /// Creates an endpoint with the given alias in place of the method name
        /// </summary>
        /// <param name="alias"></param>
        public Endpoint(string alias)
        {
            Alias = alias;
            UseMethodName = false;
        }

        /// <summary>
        /// Creates an endpoint with the given alias, with an option to use the method name as well.
        /// </summary>
        /// <param name="alias">An alias under which to register this method</param>
        /// <param name="useMethodName">Whether to also register this method using its method name.</param>
        public Endpoint(string alias, bool useMethodName)
        {
            this.Alias = alias;
            this.UseMethodName = UseMethodName;
        }


    }
}
