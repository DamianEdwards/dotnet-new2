using System;
using System.Collections.Generic;

namespace dotnet_new2
{
    public class TemplatePackage
    {
        public string Id { get; set; }

        public string Version { get; set; }

        public IList<ManifestEntry> Entries { get; set; } = new List<ManifestEntry>();

        public IList<Template> GetTemplatesList()
        {
            var templates = new List<Template>();
            PopulateTemplates(templates, Entries);
            return templates;
        }

        private void PopulateTemplates(List<Template> templates, IList<ManifestEntry> entries)
        {
            foreach (var entry in entries)
            {
                var category = entry as TemplateCategory;
                var template = entry as Template;

                if (category != null)
                {
                    PopulateTemplates(templates, category.Children);
                }
                else if (template != null)
                {
                    templates.Add(template);
                }
            }
        }

        public Template Find(Func<Template, bool> predicate)
        {
            return FindInEntries(Entries, predicate);
        }

        private Template FindInEntries(IList<ManifestEntry> entries, Func<Template, bool> predicate)
        {
            foreach (var entry in entries)
            {
                var category = entry as TemplateCategory;
                var template = entry as Template;

                if (entry != null)
                {
                    var match = FindInEntries(category.Children, predicate);
                    if (match != null)
                    {
                        return match;
                    }
                }
                else if (template != null && predicate(template))
                {
                    return template;
                }
            }

            return null;
        }
    }
}
