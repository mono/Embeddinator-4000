using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CppSharp;
using CppSharp.Generators;
using NUnit.Framework;

namespace MonoEmbeddinator4000.Tests
{
    /// <summary>
    /// A set of integration tests / approval tests verifying generated java code
    /// </summary>
    [TestFixture]
    public class DriverTest : TempFileTest
    {
        string outputDir;
        Project project;
        Options options;
        Driver driver;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            outputDir = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "output");
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            temp = Path.Combine(outputDir, "hello.dll");
            tempFiles = new List<string> { temp, outputDir };

            project = new Project();
            options = new Options
            {
                GeneratorKind = GeneratorKind.Java,
                CompileCode = true,
            };

            options.OutputDir =
                project.OutputPath = outputDir;
        }

        void RunDriver(string resourceFile, CompilerParameters parameters = null)
        {
            if (parameters == null)
            {
                parameters = new CompilerParameters();
            }
            parameters.OutputAssembly = temp;
            AssemblyGenerator.CreateFromResource(resourceFile, parameters);

            project.Assemblies.Insert(0, temp);

            driver = new Driver(project, options);
            Assert.IsTrue(driver.Run(), "Call to Driver.Run() failed!");
        }

        [Test]
        public void Empty()
        {
            driver = new Driver(new Project(), new Options
            {
                GeneratorKind = GeneratorKind.Java,
            });
            Assert.IsTrue(driver.Run()); //This runs but doesn't throw
        }

        [Test]
        public void HelloFiles()
        {
            RunDriver("Hello");

            var builder = new StringBuilder();
            foreach (var file in driver.Output.Files.Keys)
            {
                //NOTE: replace \ so this works on Windows
                builder.AppendLine(file.Replace('\\', '/'));
            }
            Approvals.Verify(builder.ToString());
        }

        [Test]
        public void Hello_java()
        {
            RunDriver("Hello");

            string path = Path.Combine(options.OutputDir, driver.Output.Files.Keys.First());
            Approvals.VerifyFile(path);
        }

        [Test]
        public void Native_hello_dll_java()
        {
            RunDriver("Hello");

            string path = Path.Combine(options.OutputDir, driver.Output.Files.Keys.Last());
            Approvals.VerifyFile(path);
        }

        [Test]
        public void UpperCase_java()
        {
            RunDriver("HelloUpper");

            string path = Path.Combine(options.OutputDir, driver.Output.Files.Keys.First());
            Approvals.VerifyFile(path); //TODO: I don't know if "String wORLD()" is what we want
        }

        [Test]
        public void AssemblyWithDots()
        {
            temp = Path.Combine(Path.GetTempPath(), "hello.with.dots.dll");
            tempFiles.Add(temp);

            RunDriver("Hello");

            string path = Path.Combine(options.OutputDir, driver.Output.Files.Keys.First());
            Approvals.VerifyFile(path);
        }

        [Test]
        public void AssemblyWithUpperCase()
        {
            temp = Path.Combine(Path.GetTempPath(), "HELLO.dll");
            tempFiles.Add(temp);

            RunDriver("Hello");

            string path = Path.Combine(options.OutputDir, driver.Output.Files.Keys.First());
            Approvals.VerifyFile(path);
        }

        [Test, Category("Slow"), Platform("MacOSX")]
        public void JarFileContents()
        {
            options.Compilation.Platform = TargetPlatform.MacOS;
            options.GeneratorKind = GeneratorKind.C;
            RunDriver("Hello");
            options.GeneratorKind = GeneratorKind.Java;
            RunDriver("Hello");

            var aar = Path.Combine(options.OutputDir, "Hello.jar");
            Approvals.VerifyZipFile(aar);
        }

        [Test, Category("Slow")]
        public void AarFileContents()
        {
            options.Compilation.Platform = TargetPlatform.Android;
            options.GeneratorKind = GeneratorKind.C;
            RunDriver("Hello");
            options.GeneratorKind = GeneratorKind.Java;
            RunDriver("Hello");

            var aar = Path.Combine(options.OutputDir, "Hello.aar");
            Approvals.VerifyZipFile(aar);
        }

        [Test, Category("Slow")]
        public void AarFileContentsDebug()
        {
            options.Compilation.Platform = TargetPlatform.Android;
            options.GeneratorKind = GeneratorKind.C;
            options.Compilation.DebugMode = true;
            RunDriver("Hello");
            options.GeneratorKind = GeneratorKind.Java;
            RunDriver("Hello");

            var aar = Path.Combine(options.OutputDir, "Hello.aar");
            Approvals.VerifyZipFile(aar);
        }

        /// <summary>
        /// NOTE: C and Java generators were failing on a subclass of EventArgs due to EventArgs.Empty
        /// </summary>
        [Test, Category("Slow")]
        public void EventArgsEmpty()
        {
            options.Compilation.Platform = TargetPlatform.Android;
            options.GeneratorKind = GeneratorKind.C;
            options.Compilation.DebugMode = true;
            RunDriver("EventArgsEmpty");
            options.GeneratorKind = GeneratorKind.Java;
            RunDriver("EventArgsEmpty");
        }

        [Test, Category("Slow")]
        public void Enums()
        {
            options.Compilation.Platform = TargetPlatform.Android;
            options.GeneratorKind = GeneratorKind.C;
            options.Compilation.DebugMode = true;
            RunDriver("Enums");
            options.GeneratorKind = GeneratorKind.Java;
            RunDriver("Enums");
        }

        [Test, Category("Slow")]
        public void Interfaces()
        {
            options.Compilation.Platform = TargetPlatform.Android;
            options.GeneratorKind = GeneratorKind.C;
            options.Compilation.DebugMode = true;
            RunDriver("Interfaces");
            options.GeneratorKind = GeneratorKind.Java;
            RunDriver("Interfaces");
        }

        /// <summary>
        /// Validates we get native libraries from the assembly
        /// </summary>
        [Test, Category("Slow")]
        public void AndroidNativeLibraries()
        {
            var nativeLibrariesZip = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "..", "..", "Samples", "__AndroidNativeLibraries__.zip");
            var parameters = new CompilerParameters();
            parameters.EmbeddedResources.Add(nativeLibrariesZip);

            options.Compilation.Platform = TargetPlatform.Android;
            options.GeneratorKind = GeneratorKind.C;
            RunDriver("Hello", parameters);
            options.GeneratorKind = GeneratorKind.Java;
            RunDriver("Hello", parameters);

            var aar = Path.Combine(options.OutputDir, "Hello.aar");
            Approvals.VerifyZipFile(aar);
        }

        /// <summary>
        /// Validates we get resources/assets from the assembly
        /// </summary>
        [Test, Category("Slow")]
        public void AndroidResources()
        {
            var libraryProjectsZip = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "..", "..", "Samples", "__AndroidLibraryProjects__.zip");
            var parameters = new CompilerParameters();
            parameters.EmbeddedResources.Add(libraryProjectsZip);

            options.Compilation.Platform = TargetPlatform.Android;
            options.GeneratorKind = GeneratorKind.C;
            RunDriver("Hello", parameters);
            options.GeneratorKind = GeneratorKind.Java;
            RunDriver("Hello", parameters);

            var aar = Path.Combine(options.OutputDir, "Hello.aar");
            Approvals.VerifyZipFile(aar);
        }

        /// <summary>
        /// Validates we get native libraries and resources/assets from a dependency
        /// NOTE: the dependent assembly should be passed as an input assembly to E4K
        /// </summary>
        [Test, Category("Slow")]
        public void AndroidDependencies()
        {
            var dependency = Path.Combine(outputDir, "dependency.dll");
            var nativeLibrariesZip = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "..", "..", "Samples", "__AndroidNativeLibraries__.zip");
            var libraryProjectsZip = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "..", "..", "Samples", "__AndroidLibraryProjects__.zip");
            var parameters = new CompilerParameters();
            parameters.OutputAssembly = dependency;
            parameters.EmbeddedResources.Add(nativeLibrariesZip);
            parameters.EmbeddedResources.Add(libraryProjectsZip);
            AssemblyGenerator.CreateFromResource("HelloUpper", parameters);
            project.Assemblies.Add(dependency);

            parameters = new CompilerParameters();
            parameters.ReferencedAssemblies.Add(dependency);
            options.Compilation.Platform = TargetPlatform.Android;
            options.GeneratorKind = GeneratorKind.C;
            RunDriver("Hello", parameters);
            options.GeneratorKind = GeneratorKind.Java;
            RunDriver("Hello", parameters);

            var aar = Path.Combine(options.OutputDir, "Hello.aar");
            Approvals.VerifyZipFile(aar);
        }
    }
}
