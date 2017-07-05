using NUnit.Framework;
using System;
using System.IO;

namespace MonoEmbeddinator4000.Tests
{
    [TestFixture]
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
            try
            {
                //Temp file
                File.Delete(generator.MainAssembly);
            }
            catch { }
        }

        [Test]
        public void String()
        {
            var assembly = Samples.LoadFile("String");
            generator.Assemblies = new[] { assembly };
            generator.MainAssembly = assembly.Location;
            generator.Generate();

            string source = generator.ToSource();
            Assert.AreNotEqual("", source);
        }

        [Test]
        public void Full()
        {
            var assembly = Samples.LoadFile("Full");
            generator.Assemblies = new[] { assembly };
            generator.MainAssembly = assembly.Location;
            generator.Generate();

            string source = generator.ToSource();
            Assert.AreNotEqual("", source);
        }
    }
}
