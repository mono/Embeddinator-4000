using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MonoEmbeddinator4000
{
    /// <summary>
    /// Represents an input file in the binding project.
    /// </summary>
    public class ProjectInput
    {
        /// <summary>
        /// Full path to the input file.
        /// </summary>
        public string FullPath;

        /// <summary>
        /// Base path to the input file.
        /// </summary>
        public string BasePath;
    }

    /// <summary>
    /// Represents the output generated for the binding project.
    /// </summary>
    public class ProjectOutput
    {
        public Dictionary<string, Stream> Files;

        public ProjectOutput()
        {
            Files = new Dictionary<string, Stream>();
        }

        public Stream GetOutput(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new NotSupportedException("Invalid path");

            var stream = new MemoryStream();
            Files[path] = stream;

            return stream;
        }

        public Stream WriteOutput(string path, string content)
        {
            var stream = GetOutput(path);

            var bytes = Encoding.UTF8.GetBytes(content);
            stream.Write(bytes, 0, bytes.Length);

            return stream;
        }
    }

    /// <summary>
    /// Represents a binding project.
    /// </summary>
    public class Project
    {
        public string Name;
        public string OutputPath;

        public List<string> AssemblyDirs;
        public List<string> Assemblies;

        internal List<ProjectInput> AssemblyInputs;

        public Project()
        {
            AssemblyDirs = new List<string>();
            Assemblies = new List<string>();
            AssemblyInputs = new List<ProjectInput>();
        }

        public void BuildInputs()
        {
            foreach (var assembly in Assemblies)
            {
                var file = Path.GetFullPath(assembly);

                if (!File.Exists(file))
                    continue;

                var input = new ProjectInput
                {
                    BasePath = Path.GetDirectoryName(file),
                    FullPath = file
                };

                AssemblyInputs.Add(input);
            }
        }

        public void SearchInputs()
        {
            foreach (var path in AssemblyDirs)
            {
                if (!Directory.Exists(path)) continue;

                var files = Directory.EnumerateFiles(path, "*.dll");

                foreach (var file in files)
                {
                    var matches = false;
                    foreach (var assembly in Assemblies)
                        matches |= Regex.IsMatch(Path.GetFileName(file), assembly);

                    if (!matches) continue;

                    var input = new ProjectInput
                        {
                            BasePath = path,
                            FullPath = file,
                        };

                    AssemblyInputs.Add(input);
                }
            }
        }
    }
}
