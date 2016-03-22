using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet_new2
{
    public class ManifestEntry
    {
        public ManifestEntry Parent { get; set; }

        public string Path { get; internal set; }

        public string Title { get; set; }
    }
}
