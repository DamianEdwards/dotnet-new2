using System;
using System.IO;

namespace dotnet_new2
{
    public class ProjectCreator
    {
        public bool CreateProject(string name, string path, Template template)
        {
            Directory.CreateDirectory(path);

            if (Directory.GetFileSystemEntries(path).Length > 0)
            {
                // Files already exist in the directory
                Console.WriteLine($"Directory {path} already contains files. Please specify a different project name.");
                return false;
            }

            foreach (var file in template.Files)
            {
                var dest = Path.Combine(path, file.DestPath);
                var destDir = Path.GetDirectoryName(dest);
                Directory.CreateDirectory(destDir);

                File.Copy(file.SourcePath, dest);
                ProcessFile(dest, name);
            }

            Console.WriteLine();
            Console.WriteLine($"Created \"{name}\" in {path}");
            Console.WriteLine();

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