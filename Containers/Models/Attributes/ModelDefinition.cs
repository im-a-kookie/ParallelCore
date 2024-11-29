namespace Containers.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ModelDefinition : Attribute
    {
        public string? Alias { get; set; }
        public ModelDefinition() { }
        public ModelDefinition(string? Alias) { }

    }
}
