using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Embeddinator.Tests
{
    /// <summary>
    /// Base class for unit tests that handle deleting temporary files
    /// </summary>
    public class TempFileTest : CurrentDirectoryTest
    {
        protected string temp;
        protected List<string> tempFiles;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            temp = Path.GetTempFileName();
            tempFiles = new List<string> { temp };
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();

            foreach (var file in tempFiles)
            {
                if (File.Exists(file))
                    File.Delete(file);
                if (Directory.Exists(file))
                    Directory.Delete(file, true);
            }
        }
    }
}
