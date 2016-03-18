using System;
using System.Diagnostics;
using System.IO;

namespace dotnet_new2
{
    public class ProjectCreator
    {
        public bool CreateProject(string name, string path, Template template)
        {
            Directory.CreateDirectory(path);

            foreach (var file in template.Files)
            {
                var dest = Path.Combine(path, file.DestPath);
                File.Copy(file.SourcePath, dest);
                ProcessFile(dest, name);
            }

            Console.WriteLine();
            Console.WriteLine($"Created \"{name}\" in {path}");

            return true;
        }

        private void ProcessFile(string destPath, string name)
        {
            // TODO: Make this good
            var contents = File.ReadAllText(destPath);

            File.WriteAllText(destPath, contents.Replace("$DefaultNamespace$", name));
        }
    }
}