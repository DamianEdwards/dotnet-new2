using System;

namespace dotnet_new2
{
    public class ProjectCreator
    {
        public bool CreateProject(string path, Template template)
        {
            Console.WriteLine();
            Console.WriteLine($"Creating project at {path} with template {template.Name}");

            return true;
        }
    }
}