using NUnit.Framework;
using System;
using ObjC;

namespace ObjCGeneratorTest {
	
	[TestFixture]
	public class Helpers {
		
		[Test]
		public void CamelCase ()
		{
			Assert.Null (ObjCGenerator.CamelCase (null), "null");
			Assert.That (ObjCGenerator.CamelCase (String.Empty), Is.EqualTo (""), "length == 0");
			Assert.That (ObjCGenerator.CamelCase ("S"), Is.EqualTo ("s"), "length == 1");
			Assert.That (ObjCGenerator.CamelCase ("TU"), Is.EqualTo ("tU"), "length == 2");
		}

		[Test]
		public void TypeMatch ()
		{
			Assert.That (ObjCGenerator.GetTypeName (typeof (bool)), Is.EqualTo ("bool"), "bool");
			Assert.That (ObjCGenerator.GetTypeName (typeof (int)), Is.EqualTo ("int"), "int");
			Assert.That (ObjCGenerator.GetTypeName (typeof (object)), Is.EqualTo ("NSObject"), "object");
		}
	}
}
