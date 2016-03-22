using System.Collections.Generic;

namespace dotnet_new2
{
    public class Template : ManifestEntry
    {
        public IList<TemplateFile> Files { get; set; } = new List<TemplateFile>();

        public TemplatePackage Package { get; set; }
    }
}
