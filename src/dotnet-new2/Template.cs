using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet_new2
{
    public class Template
    {
        public string Name { get; set; }

        public string Path { get; set; }

        public IList<TemplateFile> Files { get; set; } = new List<TemplateFile>();

        public TemplatePackage Package { get; set; }
    }
}
