using NUnit.Framework;
using System.IO;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace MonoEmbeddinator4000.Tests
{
    [TestFixture]
    public class XamarinAndroidBuildTest : UniverseTest
    {
        string manifestPath;

        public override void SetUp()
        {
            base.SetUp();

            temp = Path.Combine(Path.GetTempPath(), "hello.dll");
            manifestPath = Path.GetTempFileName();

            tempFiles = new List<string> { temp, manifestPath };
        }

        void GenerateAssembly()
        {
            var parameters = new CompilerParameters
            {
                OutputAssembly = temp,
            };
            AssemblyGenerator.CreateFromResource("Hello", parameters);
        }

        [Test]
        public void AndroidManifest()
        {
            GenerateAssembly();
            XamarinAndroidBuild.GenerateAndroidManifest(new[] { universe.LoadFile(temp) }, manifestPath, true);
            Approvals.VerifyFile(manifestPath);
        }

        [Test]
        public void AndroidManifestWithDots()
        {
            temp = Path.Combine(Path.GetTempPath(), "hello.with.dots.dll");
            tempFiles.Add(temp);

            GenerateAssembly();
            XamarinAndroidBuild.GenerateAndroidManifest(new[] { universe.LoadFile(temp) }, manifestPath, true);
            Approvals.VerifyFile(manifestPath);
        }

        [Test]
        public void AndroidManifestWithUpperCase()
        {
            temp = Path.Combine(Path.GetTempPath(), "HELLO.dll");
            tempFiles.Add(temp);

            GenerateAssembly();
            XamarinAndroidBuild.GenerateAndroidManifest(new[] { universe.LoadFile(temp) }, manifestPath, true);
            Approvals.VerifyFile(manifestPath);
        }
    }
}
