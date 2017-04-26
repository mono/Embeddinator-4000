using NUnit.Framework;
using System;

using Embeddinator;

namespace ObjCGeneratorTest {

	[TestFixture]
	public class StringExtensionsTest {

		[Test]
		public void CamelCase ()
		{
			Assert.Null ((null as string).CamelCase (), "null");
			Assert.That (String.Empty.CamelCase (), Is.EqualTo (""), "length == 0");
			Assert.That ("S".CamelCase (), Is.EqualTo ("s"), "length == 1");
			Assert.That ("TU".CamelCase (), Is.EqualTo ("tU"), "length == 2");
		}

		[Test]
		public void PascalCase ()
		{
			Assert.Null ((null as string).PascalCase (), "null");
			Assert.That (String.Empty.PascalCase (), Is.EqualTo (""), "length == 0");
			Assert.That ("s".PascalCase (), Is.EqualTo ("S"), "length == 1");
			Assert.That ("tu".PascalCase (), Is.EqualTo ("Tu"), "length == 2");
		}
	}
}
