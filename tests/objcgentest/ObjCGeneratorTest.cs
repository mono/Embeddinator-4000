using NUnit.Framework;
using System;
using ObjC;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

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
			var universe = new Universe (UniverseOptions.None);
			var asm = universe.Load ("mscorlib.dll");

			Assert.That (ObjCGenerator.GetTypeName (asm.GetType ("System.Boolean")), Is.EqualTo ("bool"), "bool");
			Assert.That (ObjCGenerator.GetTypeName (asm.GetType ("System.Int32")), Is.EqualTo ("int"), "int");
			Assert.That (ObjCGenerator.GetTypeName (asm.GetType ("System.Object")), Is.EqualTo ("NSObject"), "object");
		}
	}
}
