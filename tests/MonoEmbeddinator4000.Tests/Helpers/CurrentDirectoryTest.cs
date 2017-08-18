using System;
using System.IO;
using NUnit.Framework;

namespace MonoEmbeddinator4000.Tests
{
    /// <summary>
    /// Base unit test class for moving Environment.CurrentDirectory
    /// NOTE: this enables Driver to find /external/ or /support/
    /// </summary>
    public class CurrentDirectoryTest
    {
        string cwd;

        [SetUp]
        public virtual void SetUp()
        {
            cwd = Environment.CurrentDirectory;
            Environment.CurrentDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "..", ".."));
        }

        [TearDown]
        public virtual void TearDown()
        {
            Environment.CurrentDirectory = cwd;
        }
    }
}
