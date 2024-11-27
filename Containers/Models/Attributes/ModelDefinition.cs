using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Containers.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ModelDefinition : Attribute
    {
        public string? Alias {  get; set; }
        public ModelDefinition() { }
        public ModelDefinition(string? Alias) { }

    }
}
