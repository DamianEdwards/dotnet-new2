using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet_new2
{
    public class TemplateCategory : ManifestEntry
    {
        public IList<ManifestEntry> Children { get; set; }
    }
}
