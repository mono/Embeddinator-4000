using NUnit.Framework;
using static MonoEmbeddinator4000.ResourceDesignerGenerator.Java;

namespace MonoEmbeddinator4000.Tests
{
    /// <summary>
    /// These tests are validating ToJavaName matches what aapt creates in R.java
    /// - We can add test cases as new issues come up
    /// </summary>
    [TestFixture]
    public class JavaNameTest
    {
        [Test]
        public void CustomView()
        {
            Assert.AreEqual("customview", ToJavaName("customView"));
        }

        [Test]
        public void Theme()
        {
            Assert.AreEqual("Theme", ToJavaName("Theme"));
        }

        [Test]
        public void Theme_Hello()
        {
            Assert.AreEqual("Theme_hello", ToJavaName("Theme_hello"));
        }
    }
}
