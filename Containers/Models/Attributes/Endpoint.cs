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

        public string? Alias { get; private set; }
        public bool UseMethodName {  get; private set; }
        public Endpoint()
        {
        }

        public Endpoint(string alias)
        {
            Alias = alias;
            UseMethodName = false;
        }

        public Endpoint(string alias, bool useMethodName)
        {
            this.Alias = alias;
            this.UseMethodName = UseMethodName;
        }


    }
}
