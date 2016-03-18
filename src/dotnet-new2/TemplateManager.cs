using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.ProjectModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Frameworks;

namespace dotnet_new2
{
    public class TemplateManager
    {
        private readonly ProjectContext _templatesProjectContext;
        private readonly Project _templatesProject;

        public TemplateManager()
            : this(GetTemplatesProject())
        {

        }

        public TemplateManager(ProjectContext templatesProjectContext)
        {
            _templatesProjectContext = templatesProjectContext;
            _templatesProject = templatesProjectContext.ProjectFile;
        }

        public void InstallTemplatePackage(string packageId, string version)
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
                return;
            }

            dependencies.Add(packageId, new JValue(version));

            var json = projectFile.ToString();
            File.WriteAllText(_templatesProject.ProjectFilePath, json);

            if (!RestoreTemplatePackages())
            {
                // Error on restore! Rollback
                var origJson = origProjectFile.ToString();
                File.WriteAllText(_templatesProject.ProjectFilePath, origJson);
            }
        }

        public void UninstallTemplatePackage(string packageId)
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
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Template package {packageId} uninstalled");
        }

        public IEnumerable<TemplatePackage> GetInstalledTemplates()
        {
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

        private static ProjectContext GetTemplatesProject()
        {
            //return null;

            // TODO: Make this x-plat friendly
            var path = GetTemplatesProjectFile();
            EnsureProjectFile(path);
            var templatesProject = ProjectContext.Create(path, NuGetFramework.Parse("netstandardapp1.5"));

            return templatesProject;
        }

        private static string GetTemplatesProjectFile()
        {
            var appdata = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            var path = Path.Combine(appdata, "Microsoft", "dotnet", "cli", "data", "dotnet-new2", "templates", "project.json");

            return path;
        }

        private static void EnsureProjectFile(string path)
        {
            if (!File.Exists(GetTemplatesProjectFile()))
            {
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
