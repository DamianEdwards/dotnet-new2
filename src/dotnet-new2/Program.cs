using System;
using System.Collections.Generic;
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
            app.Description = "The dotnet-new2 command is used to create .NET Core projects using templates installed via NuGet packages.";

            app.HelpOption("-?|-h|--help");

            app.Command("list", command =>
            {
                command.Description = "Lists installed templates";

                command.OnExecute(() =>
                {
                    var packages = _templateManager.GetInstalledTemplates();

                    if (!packages.Any())
                    {
                        Console.WriteLine("No templates installed. Type 'dotnet new2 install --help' for help on installing templates.");
                        return 0;
                    }

                    foreach (var package in packages)
                    {
                        Console.WriteLine($"{package.Id} {package.Version}");

                        if (package.Templates.Any())
                        {
                            var maxNameLength = package.Templates.Max(t => t.Name.Length);

                            foreach (var template in package.Templates)
                            {
                                var padding = new string(' ', maxNameLength - template.Name.Length);
                                Console.WriteLine($"  - {template.Name} {padding} [{template.Path}]");
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

                    Console.WriteLine("Templates restored. Type 'dotnet new2 list' to list installed templates.");
                    return 0;
                });
            });

            var templateOption = app.Option("-t|--template <template/path>", "The path of the template to use", CommandOptionType.SingleValue);
            var nameOption = app.Option("-n|--name <name>", "The name of the new project", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                var name = nameOption.Value() ?? Directory.GetCurrentDirectory();
                var templatePath = templateOption.Value();
                Template template;

                if (templatePath == null)
                {
                    template = PromptForTemplate(name);
                    if (template == null)
                    {
                        Console.WriteLine("No templates installed. Type 'dotnet new2 install --help' for help on installing templates.");
                        return 1;
                    }
                }
                else
                {
                    template = _templateManager.GetTemplate(templatePath);
                    if (template == null)
                    {
                        Console.WriteLine($"The template {templatePath} wasn't found. Type 'dotnet new2 list' to list installed templates, or 'dotnet new2' to select from installed templates.");
                        return 1;
                    }
                }

                if (!_projectCreator.CreateProject(name, template))
                {
                    Console.WriteLine("Error creating project");
                    return 1;
                }

                return 0;
            });

            return app.Execute(args);
        }

        private Template PromptForTemplate(string name)
        {
            var templates = _templateManager.GetInstalledTemplates().SelectMany(tp => tp.Templates).ToList();

            if (templates.Count == 0)
            {
                return null;
            }

            // TODO: Make this support template hierarchies (recursion!)
            Console.WriteLine();
            Console.WriteLine("Templates");
            Console.WriteLine("-----------------------------------------");
            
            var maxNameLength = templates.Max(t => t.Name.Length);

            for (var i = 0; i < templates.Count; i++)
            {
                var template = templates[i];
                var padding = new string(' ', maxNameLength - template.Name.Length);
                Console.WriteLine($"{i+1}. {template.Name} {padding}[{template.Path}]");
            }

            Console.WriteLine();
            Console.Write($"Select a template [1]: ");
            Console.Write("");

            var selection = ConsoleUtils.ReadInt(templates.Count);

            Console.WriteLine($"Selected template {templates[selection-1]}");

            return templates[selection - 1];
        }
    }
}
