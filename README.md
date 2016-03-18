# dotnet-new2: Simple Project Creation for the .NET Core CLI

The `dotnet-new2` command is used to create new .NET Core projects. Projects are created using templates installed via NuGet packages.

Templates are laid out in their NuGet packages in a hierarchical fashion that enables the creation of simple trees of templates
with categories that are then merged across the installed template packages to create an overall template tree. This tree allows
the display of an interactive menu when creating new projects, while also allowing for direct project creation using a template
node's full path, e.g. `dotnet new -t aspnetcore/mvc/empty`

As templates are in normal NuGet packages and installed into the standard NuGet packages folder, they can depend on other NuGet
packages that are used in their templates, such that when they're installed, all the required packages to create new projects using
the contained templates are installed too.
