using MonoManagedToNative.Generators;
using System;
using System.IO;
using System.Reflection;

namespace MonoManagedToNative.Tests
{
    /// <summary>
    /// The main base class for a generator-based tests project.
    /// </summary>
    public abstract class TestsGenerator
    {
        readonly string name;
        readonly GeneratorKind languageKind;
        readonly Driver driver;
        readonly Options options;

        protected TestsGenerator(string name, GeneratorKind languageKind)
        {
            this.name = name;
            this.languageKind = languageKind;

            options = new Options();
            driver = new Driver(options);

            Setup();
        }

        public virtual void Setup()
        {
            var outputDir = GetOutputDirectory();
            options.OutputDir = Path.Combine(outputDir, "gen", name);
            options.LibraryName = name;

            driver.Diagnostics.Message("");
            driver.Diagnostics.Message("Generating bindings for {0} ({1})",
                options.LibraryName, options.Language.ToString());

            var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            options.Project.AssemblyDirs.Add(currentDir);
            //options.Project.Assemblies.Add(Path.Combine(currentDir, name + ".Managed.dll"));
            options.Project.Assemblies.Add(name + ".Managed.dll");
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
