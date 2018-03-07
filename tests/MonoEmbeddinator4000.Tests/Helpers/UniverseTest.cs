using IKVM.Reflection;
using NUnit.Framework;

namespace Embeddinator.Tests
{
    /// <summary>
    /// Base class for unit tests that need a Universe
    /// </summary>
    public class UniverseTest : TempFileTest
    {
        protected Universe universe;

        [SetUp]
        public override void SetUp()
        {
            universe = new Universe();

            base.SetUp();
        }

        [TearDown]
        public override void TearDown()
        {
            //Locks files on Windows if we don't dispose
            universe.Dispose();

            base.TearDown();
        }
    }
}
