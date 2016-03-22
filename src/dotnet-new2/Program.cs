using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Dnx.Runtime.Common.CommandLine;

namespace dotnet_new2
{
    public class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--debug")
            {
                args = args.Skip(1).ToArray();

                Console.WriteLine($"Waiting for debugger to attach. Process ID {Process.GetCurrentProcess().Id}");

                while (!Debugger.IsAttached)
                {
                    Thread.Sleep(100);
                }
            }

            try
            {
                return new Program(new TemplateManager(), new ProjectCreator()).Run(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: {0}", ex);
                return 1;
            }
        }

        private readonly TemplateManager _templateManager;
        private readonly ProjectCreator _projectCreator;

        public Program(TemplateManager templateManager, ProjectCreator projectCreator)
        {
            _templateManager = templateManager;
            _projectCreator = projectCreator;
        }

        public int Run(string [] args)
        {
            var app = new CommandLineApplication();
            app.Name = "dotnet-new2";
            app.Description = $"The {app.Name} command is used to create .NET Core projects using templates installed via NuGet packages.";
            app.HelpOption("-?|-h|--help");

            app.Command("list", command =>
            {
                command.Description = "Lists installed templates";

                command.OnExecute(() =>
                {
                    var packages = _templateManager.GetInstalledTemplatePackages();

                    if (!packages.Any())
                    {
                        Console.WriteLine($"No templates installed. Type '{app.Name} install --help' for help on installing templates.");
                        return 0;
                    }

                    foreach (var package in packages)
                    {
                        Console.WriteLine($"{package.Id} {package.Version}");

                        var templates = package.GetTemplatesList();
                        if (templates.Any())
                        {
                            var maxNameLength = templates.Max(t => t.Title.Length);

                            foreach (var template in templates)
                            {
                                var padding = new string(' ', maxNameLength - template.Title.Length);
                                Console.WriteLine($"  - {template.Title} {padding} [{template.Path}]");
                            }
                        }
                    }

                    return 0;
                });
            });

            app.Command("install", command =>
            {
                command.Description = "Installs templates from a package with optional version";

                var idArg = command.Argument("[PackageId]", "The ID of the template package");
                var versionArg = command.Argument("[PackageVersion]", "The version of the template package");

                command.HelpOption("-?|-h|--help");

                command.OnExecute(() =>
                {
                    if (idArg.Value == null || versionArg.Value == null)
                    {
                        // TODO: Support not having to pass the version in (likely requires NuGet API support)
                        command.ShowHelp();
                        return 2;
                    }

                    if (!_templateManager.InstallTemplatePackage(idArg.Value, versionArg.Value))
                    {
                        return 1;
                    }

                    return 0;
                });
            });

            app.Command("uninstall", command =>
            {
                command.Description = "Uninstalls a template package";

                var idArg = command.Argument("[PackageId]", "The ID of the template package");

                command.HelpOption("-?|-h|--help");

                command.OnExecute(() =>
                {
                    if (idArg.Value == null)
                    {
                        command.ShowHelp();
                        return 2;
                    }

                    if (!_templateManager.UninstallTemplatePackage(idArg.Value))
                    {
                        return 1;
                    }

                    return 0;
                });
            });

            app.Command("restore", command =>
            {
                command.Description = "Restores installed template packages";

                command.OnExecute(() =>
                {
                    if (!_templateManager.RestoreTemplatePackages())
                    {
                        Console.WriteLine("Error restoring templates.");
                        return 1;
                    }

                    Console.WriteLine($"Templates restored. Type '{app.Name} list' to list installed templates.");
                    return 0;
                });
            });

            var templateOption = app.Option("-t|--template <template/path>", "The path of the template to use", CommandOptionType.SingleValue);
            var nameOption = app.Option("-n|--name <name>", "The name of the new project", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                var templatePath = templateOption.Value();
                Template template;

                if (templatePath == null)
                {
                    template = PromptForTemplate();
                    if (template == null)
                    {
                        Console.WriteLine($"No templates installed. Type '{app.Name} install --help' for help on installing templates.");
                        return 1;
                    }
                }
                else
                {
                    template = _templateManager.GetTemplate(templatePath);
                    if (template == null)
                    {
                        Console.WriteLine($"The template {templatePath} wasn't found. Type '{app.Name} list' to list installed templates, or '{app.Name}' to select from installed templates.");
                        return 1;
                    }
                }

                string newProjectName;
                string newProjectPath;
                
                if (nameOption.Value() == null)
                {
                    if (templateOption.Value() == null)
                    {
                        // User passed no args so we're in interactive mode
                        newProjectName = PromptForName();
                        newProjectPath = Path.Combine(Directory.GetCurrentDirectory(), newProjectName);
                    }
                    else
                    {
                        // User passed the template arg but no name arg so create project in current dir
                        newProjectPath = Directory.GetCurrentDirectory();
                        newProjectName = newProjectPath.Split(Path.DirectorySeparatorChar).Last();
                    }
                }
                else
                {
                    newProjectName = nameOption.Value();
                    newProjectPath = Path.Combine(Directory.GetCurrentDirectory(), newProjectName);
                }

                if (!_projectCreator.CreateProject(newProjectName, newProjectPath, template))
                {
                    Console.WriteLine("Error creating project");
                    return 1;
                }

                return 0;
            });

            return app.Execute(args);
        }

        private Template PromptForTemplate()
        {
            var templatePackages = _templateManager.GetInstalledTemplatePackages();

            if (templatePackages.Count == 0)
            {
                return null;
            }

            var menuEntries = _templateManager.MergeManifestEntries(templatePackages);

            if (menuEntries.Count == 0)
            {
                return null;
            }

            ManifestEntry currentEntry = null;
            Template selectedTemplate = null;

            while (selectedTemplate == null)
            {
                var title = currentEntry == null ? "Templates" : currentEntry.Title + " Templates";
                Console.WriteLine();
                Console.WriteLine(title);
                Console.WriteLine("-----------------------------------------");

                var maxTitleLength = menuEntries.Max(e => e.Title.Length);

                for (var i = 0; i < menuEntries.Count; i++)
                {
                    var entry = menuEntries[i];
                    var padding = new string(' ', maxTitleLength - entry.Title.Length);
                    Console.WriteLine($"{i + 1}. {entry.Title} {padding}[{entry.Path}]");
                }

                Console.WriteLine();
                Console.Write($"Select a template [1]: ");

                var selectedNumber = ConsoleUtils.ReadInt(menuEntries.Count);
                currentEntry = menuEntries[selectedNumber - 1];
                
                var category = currentEntry as TemplateCategory;
                if (category != null)
                {
                    if (category.Children.Count == 1)
                    {
                        var firstTemplate = category.Children.FirstOrDefault() as Template;
                        if (firstTemplate != null)
                        {
                            // Only one template in this category so just pick it without prompting any further
                            selectedTemplate = firstTemplate;
                        }
                    }

                    menuEntries = category.Children;
                }
                else
                {
                    selectedTemplate = currentEntry as Template;
                }
            }

            return selectedTemplate;
        }

        private string PromptForName()
        {
            var defaultName = "Project1";

            Console.Write($"Enter a project name [{defaultName}]: ");

            var name = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(name))
            {
                name = defaultName;
            }

            return name;
        }
    }
}
