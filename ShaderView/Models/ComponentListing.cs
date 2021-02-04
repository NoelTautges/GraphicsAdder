using System.Collections.Generic;

namespace ShaderView.Models
{
    public class ComponentListing
    {
        public string Name { get; set; } = "";
        public List<ComponentListing> Children { get; set; } = new();
    }
}
