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

        public IEnumerable<TemplatePackage> GetInstalledTemplates()
        {
            if (_templatesProjectContext.LockFile == null)
            {
                RestoreTemplatePackages();
                ReloadTemplatesProject();
            }

            var templatePackages = _templatesProjectContext.LockFile.PackageLibraries;
            var exporter = _templatesProjectContext.CreateExporter("Debug");
            var packages = exporter.GetAllExports().Select(e => e.Library).OfType<PackageDescription>();
            
            foreach (var package in packages)
            {
                var manifestFile = package.Library.Files.SingleOrDefault(f => f == "templates\\templates.json");
                if (manifestFile != null)
                {
                    var templatePackage = new TemplatePackage { Id = package.Identity.Name, Version = package.Identity.Version.ToString() };
                    
                    var manifestFilePath = Path.Combine(package.Path, manifestFile).Replace('\\', Path.DirectorySeparatorChar);
                    var manifest = JObject.Parse(File.ReadAllText(manifestFilePath));

                    var projectTemplates = manifest["projectTemplates"].Children<JProperty>();

                    var templates = projectTemplates.Select(c =>
                        new Template
                        {
                            Name = c.Value["title"].ToString(),
                            Package = templatePackage,
                            Files = package.Library.Files
                                .Where(f => f.StartsWith("templates\\" + c.Name + "\\"))
                                .Select(f => new TemplateFile
                                {
                                    SourcePath = Path.Combine(package.Path, f).Replace('\\', Path.DirectorySeparatorChar),
                                    DestPath = f.Substring(("templates\\" + c.Name + "\\files\\").Length)
                                })
                                .ToList(),
                            Path = c.Name
                        });

                    templatePackage.Templates = templates.ToList();

                    yield return templatePackage;
                }
            }
        }

        public bool RestoreTemplatePackages()
        {
            var processInfo = new ProcessStartInfo("dotnet.exe", "restore");
            processInfo.WorkingDirectory = Path.GetDirectoryName(_templatesProject.ProjectFilePath);
            var restore = Process.Start(processInfo);
            restore.WaitForExit();

            return restore.ExitCode == 0;
        }

        private void ReloadTemplatesProject()
        {
            _templatesProjectContext = GetTemplatesProject(_templatesProjectContext.ProjectFile.ProjectFilePath);
            _templatesProject = _templatesProjectContext.ProjectFile;
        }

        internal Template GetTemplate(string path)
        {
            var template = GetInstalledTemplates()
                .SelectMany(p => p.Templates)
                .Where(t => t.Path == path)
                .FirstOrDefault();

            return template;
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
