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

            var templateManager = new TemplateManager();

            return new Program().Run(args, templateManager);
        }

        public int Run(string [] args, TemplateManager templateManager)
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
                    var packages = templateManager.GetInstalledTemplates();

                    Console.WriteLine();

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

                    Console.WriteLine();

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
                    if (idArg.Value == null)
                    {
                        command.ShowHelp();
                        return 2;
                    }

                    templateManager.InstallTemplatePackage(idArg.Value, versionArg.Value);

                    return 0;
                });
            });

            app.Command("uninstall", command =>
            {
                command.Description = "Uninstalls a templates package";

                var idArg = command.Argument("[PackageId]", "The ID of the template package");

                command.HelpOption("-?|-h|--help");

                command.OnExecute(() =>
                {
                    if (idArg.Value == null)
                    {
                        command.ShowHelp();
                        return 2;
                    }

                    templateManager.UninstallTemplatePackage(idArg.Value);

                    return 0;
                });
            });

            app.Command("restore", command =>
            {
                command.Description = "Restores installed template packages";

                command.OnExecute(() =>
                {
                    templateManager.RestoreTemplatePackages();

                    return 0;
                });
            });

            var templateOption = app.Option("-t|--template <template/path>", "The path of the template to use", CommandOptionType.SingleValue);
            var nameOption = app.Option("-n|--name <name>", "The name of the new project", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                var name = nameOption.Value() ?? Directory.GetCurrentDirectory();

                if (templateOption.Value() == null)
                {
                    ShowMenu(name);

                    return 0;
                }

                CreateProject(templateOption.Value(), name);

                return 0;
            });

            return app.Execute(args);
        }

        private void ShowMenu(string name)
        {
            // TODO: Make this recursive, and real, etc.
            
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Templates");
            Console.WriteLine("-----------------------------------------");
            Console.WriteLine("TODO: Build and show the real templates menu");

            var templates = GetInstalledTemplates();

            for (var i = 0; i < templates.Count; i++)
            {
                Console.WriteLine($"{i+1}. {templates[i]}");
            }

            Console.WriteLine();
            Console.Write($"Select a template [1]: ");
            Console.Write("");

            var selection = ConsoleUtils.ReadInt(templates.Count);

            Console.WriteLine($"Selected template {templates[selection-1]}");
        }

        private void CreateProject(string template, string name)
        {
            Console.WriteLine($"TODO: Generate new project '{name}' using template '{template}'");
        }

        private List<string> GetInstalledTemplates()
        {
            // TODO: Build the templates list from the installed template packages

            return new List<string>
            {
                "Console Application [console]",
                "Class Library       [classlib]",
                "ASP.NET Core        [aspnetcore]"
            };
        }
    }
}
