using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Embeddinator.Tests
{
    [TestFixture]
    public class HelpersTests : TempFileTest
    {
        string currentDirectory;
        string testDirectory;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            currentDirectory = Environment.CurrentDirectory;

            testDirectory = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "test");
            if (!Directory.Exists(testDirectory))
                Directory.CreateDirectory(testDirectory);
            Environment.CurrentDirectory = testDirectory;

            tempFiles = new List<string> { testDirectory };
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();

            Environment.CurrentDirectory = currentDirectory;
        }

        [Test]
        public void FindInCurrentDirectory()
        {
            Directory.CreateDirectory("foo");

            DirectoryAssert.Exists(Helpers.FindDirectory("foo"));
        }

        [Test]
        public void FindAboveCurrentDirectory()
        {
            Directory.CreateDirectory("foo");
            Directory.CreateDirectory("bar");
            Environment.CurrentDirectory = Path.Combine(Environment.CurrentDirectory, "bar");

            DirectoryAssert.Exists(Helpers.FindDirectory("foo"));
        }

        [Test]
        public void FindRelativeToAssembly()
        {
            Environment.CurrentDirectory = Path.GetTempPath();

            DirectoryAssert.Exists(Helpers.FindDirectory("test"));
        }
    }
}
