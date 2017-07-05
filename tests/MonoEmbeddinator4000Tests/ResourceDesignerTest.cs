using System;
using System.IO;
using ApprovalTests;
using ApprovalTests.Reporters;
using NUnit.Framework;

namespace MonoEmbeddinator4000.Tests
{
    /// <summary>
    /// These are a set of integration/approval tests validating that we are getting expected C# code from ResourceDesignerGenerator
    /// </summary>
    [TestFixture, UseReporter(typeof(NUnitReporter))]
    public class ResourceDesignerTest
    {
        ResourceDesignerGenerator generator;

        [SetUp]
        public void SetUp()
        {
            generator = new ResourceDesignerGenerator
            {
                MonoDroidPath = Samples.MonoDroidPath,
                OutputDirectory = Environment.CurrentDirectory,
                PackageName = "com.mono.embeddinator",
            };
        }

        [TearDown]
        public void TearDown()
        {
            //Temp file
            File.Delete(generator.MainAssembly);
        }

        void LoadAndGenerate(string resourceFile)
        {
            var assembly = Samples.LoadFile(resourceFile);
            generator.Assemblies = new[] { assembly };
            generator.MainAssembly = assembly.Location;
            generator.Generate();
        }

        [Test]
        public void String()
        {
            LoadAndGenerate("String");

            string source = generator.ToSource();
            Approvals.Verify(source);
        }

        [Test]
        public void Full()
        {
            LoadAndGenerate("Full");

            string source = generator.ToSource();
            Approvals.Verify(source);
        }
    }
}
