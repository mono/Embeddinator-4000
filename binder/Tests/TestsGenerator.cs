using CppSharp;
using CppSharp.Generators;
using System;
using System.IO;
using System.Reflection;

namespace MonoEmbeddinator4000.Tests
{
    /// <summary>
    /// The main base class for a generator-based tests project.
    /// </summary>
    public abstract class TestsGenerator
    {
        readonly string name;
        readonly Driver driver;
        readonly Options options;
        readonly Project project;

        protected TestsGenerator(string name, GeneratorKind languageKind)
        {
            this.name = name;

            options = new Options();
            options.GeneratorKind = languageKind;
            project = new Project();
            driver = new Driver(project, options);

            Setup();
        }

        public virtual void Setup()
        {
            var outputDir = GetOutputDirectory();
            options.OutputDir = Path.Combine(outputDir, "gen", name);
            options.LibraryName = name;

            Diagnostics.Message("");
            Diagnostics.Message("Generating bindings for {0} ({1})",
                options.LibraryName, options.GeneratorKind.ToString());

            var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            project.Assemblies.Add(Path.Combine(currentDir, name + ".Managed.dll"));
        }

        public virtual void Generate()
        {
            driver.Run();
        }

        #region Helpers
        public static string GetTestsDirectory(string name)
        {
            var directory = Directory.GetParent(Directory.GetCurrentDirectory());

            while (directory != null)
            {
                var path = Path.Combine(directory.FullName, "tests", name);

                if (Directory.Exists(path))
                    return path;

                directory = directory.Parent;
            }

            throw new Exception(string.Format(
                "Tests directory for project '{0}' was not found", name));
        }

        static string GetOutputDirectory()
        {
            var directory = Directory.GetParent(Directory.GetCurrentDirectory());

            while (directory != null)
            {
                var path = Path.Combine(directory.FullName, "obj");

                if (Directory.Exists(path))
                    return directory.FullName;

                directory = directory.Parent;
            }

            throw new Exception("Could not find tests output directory");
        }
        #endregion
    }
}
