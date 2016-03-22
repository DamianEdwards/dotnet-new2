using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.DotNet.ProjectModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Frameworks;

namespace dotnet_new2
{
    public class TemplateManager
    {
        private ProjectContext _templatesProjectContext;
        private Project _templatesProject;

        // TODO: Don't Console.Write from this class, pass in a logger to write to or something and use pretty colors

        public TemplateManager()
            : this(GetTemplatesProject(GetDefaultTemplatesProjectFile()))
        {

        }

        public TemplateManager(ProjectContext templatesProjectContext)
        {
            _templatesProjectContext = templatesProjectContext;
            _templatesProject = templatesProjectContext.ProjectFile;
        }

        public bool InstallTemplatePackage(string packageId, string version)
        {
            // TODO: Validate the package ID and version

            // Add the dependency to the templates project file
            var projectFile = JObject.Parse(File.ReadAllText(_templatesProject.ProjectFilePath));
            var origProjectFile = projectFile.DeepClone();

            var dependencies = projectFile.Value<JObject>("dependencies");

            if (dependencies.Property(packageId) != null)
            {
                // Already installed!
                Console.WriteLine($"Template package {packageId} is already installed");
                return true;
            }

            dependencies.Add(packageId, new JValue(version));

            var json = projectFile.ToString();
            File.WriteAllText(_templatesProject.ProjectFilePath, json);

            if (!RestoreTemplatePackages())
            {
                // Error on restore! Rollback
                var origJson = origProjectFile.ToString();
                File.WriteAllText(_templatesProject.ProjectFilePath, origJson);

                Console.WriteLine($"Error installing template package {packageId}");
                return false;
            }

            Console.WriteLine($"Template package {packageId} installed");
            return true;
        }

        public bool UninstallTemplatePackage(string packageId)
        {
            // Remove the dependency from the templates project file
            var projectFile = JObject.Parse(File.ReadAllText(_templatesProject.ProjectFilePath));
            var origProjectFile = projectFile.DeepClone();

            var dependencies = projectFile.Value<JObject>("dependencies");

            if (dependencies.Property(packageId) != null)
            {
                dependencies.Remove(packageId);

                var json = projectFile.ToString();
                File.WriteAllText(_templatesProject.ProjectFilePath, json);

                if (!RestoreTemplatePackages())
                {
                    // Error on restore! Rollback
                    var origJson = origProjectFile.ToString();
                    File.WriteAllText(_templatesProject.ProjectFilePath, origJson);

                    Console.WriteLine($"Error uninstalling template package {packageId}");
                    return false;
                }
            }

            Console.WriteLine($"Template package {packageId} uninstalled");
            return true;
        }

        public IList<TemplatePackage> GetInstalledTemplatePackages()
        {
            if (_templatesProjectContext.LockFile == null)
            {
                RestoreTemplatePackages();
                ReloadTemplatesProject();
            }

            var templatePackages = _templatesProjectContext.LockFile.PackageLibraries;
            var exporter = _templatesProjectContext.CreateExporter("Debug");
            var packages = exporter.GetAllExports().Select(e => e.Library).OfType<PackageDescription>();

            var result = new List<TemplatePackage>();

            foreach (var package in packages)
            {
                var manifestFile = package.Library.Files.SingleOrDefault(f => f == "templates\\templates.json");

                // TODO: Handle other packages turning up in lock file due to template packages having dependencies
                if (manifestFile != null)
                {
                    var templatePackage = new TemplatePackage { Id = package.Identity.Name, Version = package.Identity.Version.ToString() };
                    
                    var manifestFilePath = Path.Combine(package.Path, manifestFile).Replace('\\', Path.DirectorySeparatorChar);
                    var manifest = JObject.Parse(File.ReadAllText(manifestFilePath));

                    var manifestEntries = manifest["projectTemplates"].Children<JProperty>();

                    templatePackage.Entries = manifestEntries
                        .Select(e => ProcessManifestEntry(null, e, package, templatePackage))
                        .ToList();

                    result.Add(templatePackage);
                }
            }

            return result;
        }

        public IList<ManifestEntry> MergeManifestEntries(IList<TemplatePackage> templatePackages)
        {
            var entries = new List<ManifestEntry>(templatePackages.First().Entries);

            foreach (var package in templatePackages.Skip(1))
            {
                MergeManifestEntries(package.Entries, entries);
            }

            return entries;
        }

        public bool RestoreTemplatePackages()
        {
            var processInfo = new ProcessStartInfo("dotnet.exe", "restore");
            processInfo.WorkingDirectory = Path.GetDirectoryName(_templatesProject.ProjectFilePath);
            var restore = Process.Start(processInfo);
            restore.WaitForExit();

            return restore.ExitCode == 0;
        }

        public Template GetTemplate(string path)
        {
            var packages = GetInstalledTemplatePackages();

            foreach (var package in packages)
            {
                var match = package.Find(t => t.Path == path);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private void ReloadTemplatesProject()
        {
            _templatesProjectContext = GetTemplatesProject(_templatesProjectContext.ProjectFile.ProjectFilePath);
            _templatesProject = _templatesProjectContext.ProjectFile;
        }

        private static ManifestEntry ProcessManifestEntry(ManifestEntry parent, JProperty entry, PackageDescription package, TemplatePackage templatePackage)
        {
            var title = entry.Value["title"].ToString();
            var children = entry.Value["children"];
            var path = parent == null ? entry.Name : Path.Combine(parent.Path, entry.Name);

            if (children != null)
            {
                // Entry is a category
                var category = new TemplateCategory
                {
                    Parent = parent,
                    Path = path,
                    Title = title
                };
                category.Children = children.Children<JProperty>()
                    .Select(e => ProcessManifestEntry(category, e, package, templatePackage))
                    .ToList();
                return category;
            }

            // Entry is a template
            
            return new Template
            {
                Parent = parent,
                Path = path,
                Title = title,
                Package = templatePackage,
                Files = package.Library.Files
                    .Where(f => f.StartsWith("templates\\" + path + "\\"))
                    .Select(f => new TemplateFile
                    {
                        SourcePath = Path.Combine(package.Path, f).Replace('\\', Path.DirectorySeparatorChar),
                        DestPath = f.Substring(("templates\\" + path + "\\files\\").Length)
                    })
                    .ToList()
            };
        }

        private static void MergeManifestEntries(IList<ManifestEntry> source, IList<ManifestEntry> dest)
        {
            foreach (var entry in source)
            {
                var match = dest.FirstOrDefault(e => e.Path == entry.Path);
                if (match != null)
                {
                    // Matching entry found, perform the merge
                    var matchTemplate = match as Template;
                    var matchCategory = match as TemplateCategory;
                    var template = entry as Template;
                    var category = entry as TemplateCategory;

                    if (template != null)
                    {
                        // It's a template in the source so just add it to the dest as a dupe
                        dest.Add(template);
                    }
                    else if (category != null)
                    {
                        // It's a category in the source
                        if (matchCategory != null)
                        {
                            // Source and dest are both categories so merge them
                            MergeManifestEntries(category.Children, matchCategory.Children);
                        }
                        else if (matchTemplate != null)
                        {
                            // Source is a category but dest is a template so just add the category as a dupe
                            dest.Add(category);
                        }
                    }
                }
                else
                {
                    // New entry, just add it
                    dest.Add(entry);
                }
            }
        }

        private static ProjectContext GetTemplatesProject(string path)
        {   
            EnsureProjectFile(path);
            var templatesProject = ProjectContext.Create(path, NuGetFramework.Parse("netstandardapp1.5"));

            return templatesProject;
        }

        private static string GetDefaultTemplatesProjectFile()
        {
            // TODO: Make this x-plat friendly
            var appdata = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            var path = Path.Combine(appdata, "Microsoft", "dotnet", "cli", "data", "dotnet-new2", "templates", "project.json");

            return path;
        }

        private static void EnsureProjectFile(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!File.Exists(path))
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var file = File.CreateText(path))
                {
                    file.WriteLine(
@"{
  ""dependencies"": {
    
  },
  ""frameworks"": {
    ""netstandardapp1.5"": { }
  }
}"
                    );
                }
            }
        }
    }
}
