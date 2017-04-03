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
			Assert.That (ObjCGenerator.GetTypeName (asm.GetType ("System.Char")), Is.EqualTo ("unsigned short"), "char");
			Assert.That (ObjCGenerator.GetTypeName (asm.GetType ("System.SByte")), Is.EqualTo ("signed char"), "sbyte");
			Assert.That (ObjCGenerator.GetTypeName (asm.GetType ("System.Int16")), Is.EqualTo ("short"), "short");
			Assert.That (ObjCGenerator.GetTypeName (asm.GetType ("System.Int32")), Is.EqualTo ("int"), "int");
			Assert.That (ObjCGenerator.GetTypeName (asm.GetType ("System.Int64")), Is.EqualTo ("long long"), "long");
			Assert.That (ObjCGenerator.GetTypeName (asm.GetType ("System.Byte")), Is.EqualTo ("unsigned char"), "byte");
			Assert.That (ObjCGenerator.GetTypeName (asm.GetType ("System.UInt16")), Is.EqualTo ("unsigned short"), "ushort");
			Assert.That (ObjCGenerator.GetTypeName (asm.GetType ("System.UInt32")), Is.EqualTo ("unsigned int"), "uint");
			Assert.That (ObjCGenerator.GetTypeName (asm.GetType ("System.UInt64")), Is.EqualTo ("unsigned long long"), "ulong");
			Assert.That (ObjCGenerator.GetTypeName (asm.GetType ("System.Single")), Is.EqualTo ("float"), "float");
			Assert.That (ObjCGenerator.GetTypeName (asm.GetType ("System.Double")), Is.EqualTo ("double"), "double");
			Assert.That (ObjCGenerator.GetTypeName (asm.GetType ("System.String")), Is.EqualTo ("NSString*"), "string");
			Assert.That (ObjCGenerator.GetTypeName (asm.GetType ("System.Object")), Is.EqualTo ("NSObject"), "object");
		}

		[Test]
		public void TypeMatchFailure ()
		{
			var universe = new Universe (UniverseOptions.None);
			var asm = universe.Load ("mscorlib.dll");

			Assert.Throws<NotImplementedException> (() => ObjCGenerator.GetTypeName (asm.GetType ("System.DateTime")), "DateTime");
		}
	}
}
