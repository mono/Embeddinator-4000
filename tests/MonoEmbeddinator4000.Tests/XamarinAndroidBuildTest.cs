using NUnit.Framework;
using System.IO;
using System.CodeDom.Compiler;
using IKVM.Reflection;

namespace MonoEmbeddinator4000.Tests
{
    [TestFixture]
    public class XamarinAndroidBuildTest
    {
        Universe universe;
        string temp = Path.Combine(Path.GetTempPath(), "hello.dll");

        [SetUp]
        public void SetUp()
        {
            universe = new Universe();
        }

        [TearDown]
        public void TearDown()
        {
            //Locks files on Windows
            universe.Dispose();

            if (File.Exists(temp))
                File.Delete(temp);
        }

        [Test]
        public void AndroidManifest()
        {
            var parameters = new CompilerParameters
            {
                OutputAssembly = temp,
            };
            AssemblyGenerator.CreateFromResource("Hello", parameters);

            var assembly = universe.LoadFile(temp);
            var manifestPath = Path.GetTempFileName();
            try
            {
                XamarinAndroidBuild.GenerateAndroidManifest(new[] { assembly }, manifestPath, true);

                Approvals.Verify(File.ReadAllText(manifestPath));
            }
            finally
            {
                File.Delete(manifestPath);
            }
        }
    }
}
