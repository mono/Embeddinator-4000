using NUnit.Framework;
using System;
using ObjC;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

namespace ObjCGeneratorTest {
	
	[TestFixture]
	public class Helpers {

		public static Universe Universe { get; } = new Universe (UniverseOptions.None);

		public static Assembly mscorlib { get; } = Universe.Load ("mscorlib.dll");

		[Test]
		public void CamelCase ()
		{
			Assert.Null (ObjCGenerator.CamelCase (null), "null");
			Assert.That (ObjCGenerator.CamelCase (String.Empty), Is.EqualTo (""), "length == 0");
			Assert.That (ObjCGenerator.CamelCase ("S"), Is.EqualTo ("s"), "length == 1");
			Assert.That (ObjCGenerator.CamelCase ("TU"), Is.EqualTo ("tU"), "length == 2");
		}

		[Test]
		public void PascalCase ()
		{
			Assert.Null (ObjCGenerator.PascalCase (null), "null");
			Assert.That (ObjCGenerator.PascalCase (String.Empty), Is.EqualTo (""), "length == 0");
			Assert.That (ObjCGenerator.PascalCase ("s"), Is.EqualTo ("S"), "length == 1");
			Assert.That (ObjCGenerator.PascalCase ("tu"), Is.EqualTo ("Tu"), "length == 2");
		}

		[Test]
		public void TypeMatch ()
		{
			Assert.That (ObjCGenerator.GetTypeName (mscorlib.GetType ("System.Boolean")), Is.EqualTo ("bool"), "bool");
			Assert.That (ObjCGenerator.GetTypeName (mscorlib.GetType ("System.Char")), Is.EqualTo ("unsigned short"), "char");
			Assert.That (ObjCGenerator.GetTypeName (mscorlib.GetType ("System.SByte")), Is.EqualTo ("signed char"), "sbyte");
			Assert.That (ObjCGenerator.GetTypeName (mscorlib.GetType ("System.Int16")), Is.EqualTo ("short"), "short");
			Assert.That (ObjCGenerator.GetTypeName (mscorlib.GetType ("System.Int32")), Is.EqualTo ("int"), "int");
			Assert.That (ObjCGenerator.GetTypeName (mscorlib.GetType ("System.Int64")), Is.EqualTo ("long long"), "long");
			Assert.That (ObjCGenerator.GetTypeName (mscorlib.GetType ("System.Byte")), Is.EqualTo ("unsigned char"), "byte");
			Assert.That (ObjCGenerator.GetTypeName (mscorlib.GetType ("System.UInt16")), Is.EqualTo ("unsigned short"), "ushort");
			Assert.That (ObjCGenerator.GetTypeName (mscorlib.GetType ("System.UInt32")), Is.EqualTo ("unsigned int"), "uint");
			Assert.That (ObjCGenerator.GetTypeName (mscorlib.GetType ("System.UInt64")), Is.EqualTo ("unsigned long long"), "ulong");
			Assert.That (ObjCGenerator.GetTypeName (mscorlib.GetType ("System.Single")), Is.EqualTo ("float"), "float");
			Assert.That (ObjCGenerator.GetTypeName (mscorlib.GetType ("System.Double")), Is.EqualTo ("double"), "double");
			Assert.That (ObjCGenerator.GetTypeName (mscorlib.GetType ("System.String")), Is.EqualTo ("NSString *"), "string");
			Assert.That (ObjCGenerator.GetTypeName (mscorlib.GetType ("System.Object")), Is.EqualTo ("NSObject"), "object");
			Assert.That (ObjCGenerator.GetTypeName (mscorlib.GetType ("System.Void")), Is.EqualTo ("void"), "void");
		}

		[Test]
		public void TypeMatchFailure ()
		{
			Assert.Throws<NotImplementedException> (() => ObjCGenerator.GetTypeName (mscorlib.GetType ("System.DateTime")), "DateTime");
		}

		[Test]
		public void MonoMatch ()
		{
			Assert.That (ObjCGenerator.GetMonoName (mscorlib.GetType ("System.Boolean")), Is.EqualTo ("bool"), "bool");
			Assert.That (ObjCGenerator.GetMonoName (mscorlib.GetType ("System.Char")), Is.EqualTo ("char"), "char");
			Assert.That (ObjCGenerator.GetMonoName (mscorlib.GetType ("System.SByte")), Is.EqualTo ("sbyte"), "sbyte");
			Assert.That (ObjCGenerator.GetMonoName (mscorlib.GetType ("System.Int16")), Is.EqualTo ("int16"), "short");
			Assert.That (ObjCGenerator.GetMonoName (mscorlib.GetType ("System.Int32")), Is.EqualTo ("int"), "int");
			Assert.That (ObjCGenerator.GetMonoName (mscorlib.GetType ("System.Int64")), Is.EqualTo ("long"), "long");
			Assert.That (ObjCGenerator.GetMonoName (mscorlib.GetType ("System.Byte")), Is.EqualTo ("byte"), "byte");
			Assert.That (ObjCGenerator.GetMonoName (mscorlib.GetType ("System.UInt16")), Is.EqualTo ("uint16"), "ushort");
			Assert.That (ObjCGenerator.GetMonoName (mscorlib.GetType ("System.UInt32")), Is.EqualTo ("uint"), "uint");
			Assert.That (ObjCGenerator.GetMonoName (mscorlib.GetType ("System.UInt64")), Is.EqualTo ("ulong"), "ulong");
			Assert.That (ObjCGenerator.GetMonoName (mscorlib.GetType ("System.Single")), Is.EqualTo ("single"), "float");
			Assert.That (ObjCGenerator.GetMonoName (mscorlib.GetType ("System.Double")), Is.EqualTo ("double"), "double");
			Assert.That (ObjCGenerator.GetMonoName (mscorlib.GetType ("System.String")), Is.EqualTo ("string"), "string");
			Assert.That (ObjCGenerator.GetMonoName (mscorlib.GetType ("System.Void")), Is.EqualTo ("void"), "void");
			Assert.That (ObjCGenerator.GetMonoName (mscorlib.GetType ("System.Object")), Is.EqualTo ("object"), "object");
		}

		[Test]
		public void GetObjCName ()
		{
			Assert.That (ObjCGenerator.GetObjCName (mscorlib.GetType ("System.Collections.ArrayList")), Is.EqualTo ("System_Collections_ArrayList"), "ArrayList");
		}
	}
}
