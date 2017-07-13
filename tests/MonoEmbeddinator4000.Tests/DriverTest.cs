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
        Project project;
        Options options;
        Driver driver;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            var outputDir = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "output");
            temp = Path.Combine(Path.GetTempPath(), "hello.dll");
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

        void RunDriver(string resourceFile)
        {
            var parameters = new CompilerParameters
            {
                OutputAssembly = temp,
            };
            AssemblyGenerator.CreateFromResource(resourceFile, parameters);

            project.Assemblies.Add(temp);

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
    }
}
