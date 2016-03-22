using System.Collections.Generic;

namespace dotnet_new2
{
    public class TemplateCategory : ManifestEntry
    {
        public IList<ManifestEntry> Children { get; set; }
    }
}
