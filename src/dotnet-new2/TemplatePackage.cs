using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet_new2
{
    public class TemplatePackage
    {
        public string Id { get; set; }

        public string Version { get; set; }

        public IList<Template> Templates { get; set; } = new List<Template>();
    }
}
