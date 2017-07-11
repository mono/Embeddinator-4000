using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Text;
using CppSharp.Generators;
using NUnit.Framework;

namespace MonoEmbeddinator4000.Tests
{
    /// <summary>
    /// A set of integration tests / approval tests verifying generated java code
    /// </summary>
    [TestFixture]
    public class DriverTest
    {
        string temp = Path.Combine(Path.GetTempPath(), "hello.dll");
        Project project;
        Options options;
        Driver driver;

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }

        void RunDriver(string resourceFile)
        {
            var parameters = new CompilerParameters
            {
                OutputAssembly = temp,
            };
            AssemblyGenerator.CreateFromResource(resourceFile, parameters);

            project = new Project();
            options = new Options
            {
                GeneratorKind = GeneratorKind.Java,
            };

            options.OutputDir =
                project.OutputPath = Path.GetDirectoryName(GetType().Assembly.Location);
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
            string java = File.ReadAllText(path);
            Approvals.Verify(java);
        }

        [Test]
        public void Native_hello_dll_java()
        {
            RunDriver("Hello");

            string path = Path.Combine(options.OutputDir, driver.Output.Files.Keys.Last());
            string java = File.ReadAllText(path);
            Approvals.Verify(java);
        }

        [Test]
        public void UpperCase_java()
        {
            RunDriver("HelloUpper");

            string path = Path.Combine(options.OutputDir, driver.Output.Files.Keys.First());
            string java = File.ReadAllText(path);
            Approvals.Verify(java); //TODO: I don't know if "String wORLD()" is what we want
        }
    }
}
