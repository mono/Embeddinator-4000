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
		public void TypeMatch ()
		{
			Assert.That (NameGenerator.GetTypeName (mscorlib.GetType ("System.Boolean")), Is.EqualTo ("bool"), "bool");
			Assert.That (NameGenerator.GetTypeName (mscorlib.GetType ("System.Char")), Is.EqualTo ("unsigned short"), "char");
			Assert.That (NameGenerator.GetTypeName (mscorlib.GetType ("System.SByte")), Is.EqualTo ("signed char"), "sbyte");
			Assert.That (NameGenerator.GetTypeName (mscorlib.GetType ("System.Int16")), Is.EqualTo ("short"), "short");
			Assert.That (NameGenerator.GetTypeName (mscorlib.GetType ("System.Int32")), Is.EqualTo ("int"), "int");
			Assert.That (NameGenerator.GetTypeName (mscorlib.GetType ("System.Int64")), Is.EqualTo ("long long"), "long");
			Assert.That (NameGenerator.GetTypeName (mscorlib.GetType ("System.Byte")), Is.EqualTo ("unsigned char"), "byte");
			Assert.That (NameGenerator.GetTypeName (mscorlib.GetType ("System.UInt16")), Is.EqualTo ("unsigned short"), "ushort");
			Assert.That (NameGenerator.GetTypeName (mscorlib.GetType ("System.UInt32")), Is.EqualTo ("unsigned int"), "uint");
			Assert.That (NameGenerator.GetTypeName (mscorlib.GetType ("System.UInt64")), Is.EqualTo ("unsigned long long"), "ulong");
			Assert.That (NameGenerator.GetTypeName (mscorlib.GetType ("System.Single")), Is.EqualTo ("float"), "float");
			Assert.That (NameGenerator.GetTypeName (mscorlib.GetType ("System.Double")), Is.EqualTo ("double"), "double");
			Assert.That (NameGenerator.GetTypeName (mscorlib.GetType ("System.String")), Is.EqualTo ("NSString *"), "string");
			Assert.That (NameGenerator.GetTypeName (mscorlib.GetType ("System.Object")), Is.EqualTo ("NSObject"), "object");
			Assert.That (NameGenerator.GetTypeName (mscorlib.GetType ("System.Void")), Is.EqualTo ("void"), "void");
		}

		[Test]
		public void TypeMatchFailure ()
		{
			Assert.Throws<NotImplementedException> (() => NameGenerator.GetTypeName (mscorlib.GetType ("System.DBNull")), "DBNull");
		}

		[Test]
		public void MonoMatch ()
		{
			Assert.That (NameGenerator.GetMonoName (mscorlib.GetType ("System.Boolean")), Is.EqualTo ("bool"), "bool");
			Assert.That (NameGenerator.GetMonoName (mscorlib.GetType ("System.Char")), Is.EqualTo ("char"), "char");
			Assert.That (NameGenerator.GetMonoName (mscorlib.GetType ("System.SByte")), Is.EqualTo ("sbyte"), "sbyte");
			Assert.That (NameGenerator.GetMonoName (mscorlib.GetType ("System.Int16")), Is.EqualTo ("int16"), "short");
			Assert.That (NameGenerator.GetMonoName (mscorlib.GetType ("System.Int32")), Is.EqualTo ("int"), "int");
			Assert.That (NameGenerator.GetMonoName (mscorlib.GetType ("System.Int64")), Is.EqualTo ("long"), "long");
			Assert.That (NameGenerator.GetMonoName (mscorlib.GetType ("System.Byte")), Is.EqualTo ("byte"), "byte");
			Assert.That (NameGenerator.GetMonoName (mscorlib.GetType ("System.UInt16")), Is.EqualTo ("uint16"), "ushort");
			Assert.That (NameGenerator.GetMonoName (mscorlib.GetType ("System.UInt32")), Is.EqualTo ("uint"), "uint");
			Assert.That (NameGenerator.GetMonoName (mscorlib.GetType ("System.UInt64")), Is.EqualTo ("ulong"), "ulong");
			Assert.That (NameGenerator.GetMonoName (mscorlib.GetType ("System.Single")), Is.EqualTo ("single"), "float");
			Assert.That (NameGenerator.GetMonoName (mscorlib.GetType ("System.Double")), Is.EqualTo ("double"), "double");
			Assert.That (NameGenerator.GetMonoName (mscorlib.GetType ("System.String")), Is.EqualTo ("string"), "string");
			Assert.That (NameGenerator.GetMonoName (mscorlib.GetType ("System.Void")), Is.EqualTo ("void"), "void");
			Assert.That (NameGenerator.GetMonoName (mscorlib.GetType ("System.Object")), Is.EqualTo ("object"), "object");
		}

		[Test]
		public void GetObjCName ()
		{
			Assert.That (NameGenerator.GetObjCName (mscorlib.GetType ("System.Collections.ArrayList")), Is.EqualTo ("System_Collections_ArrayList"), "ArrayList");
		}

		[Test]
		public void FormatRawValue ()
		{
			var ts = mscorlib.GetType ("System.String");
			Assert.That (ObjCGenerator.FormatRawValue (ts, null), Is.EqualTo ("nil"), "null");
			Assert.That (ObjCGenerator.FormatRawValue (ts, String.Empty), Is.EqualTo ("@\"\""), "String Empty");
			var tf = mscorlib.GetType ("System.Single");
			Assert.That (ObjCGenerator.FormatRawValue (tf, Single.NaN), Is.EqualTo ("NAN"), "Single NaN");
			Assert.That (ObjCGenerator.FormatRawValue (tf, Single.PositiveInfinity), Is.EqualTo ("INFINITY"), "Single PositiveInfinity");
			Assert.That (ObjCGenerator.FormatRawValue (tf, Single.NegativeInfinity), Is.EqualTo ("INFINITY"), "Single NegativeInfinity");
			Assert.That (ObjCGenerator.FormatRawValue (tf, (float)Math.E), Is.EqualTo ("2.718282f"), "Single E");
			var td = mscorlib.GetType ("System.Double");
			Assert.That (ObjCGenerator.FormatRawValue (td, Double.NaN), Is.EqualTo ("NAN"), "Double NaN");
			Assert.That (ObjCGenerator.FormatRawValue (td, Double.PositiveInfinity), Is.EqualTo ("INFINITY"), "Double PositiveInfinity");
			Assert.That (ObjCGenerator.FormatRawValue (td, Double.NegativeInfinity), Is.EqualTo ("INFINITY"), "Double NegativeInfinity");
			Assert.That (ObjCGenerator.FormatRawValue (td, Math.PI), Is.EqualTo ("3.14159265358979d"), "Double PI");

			var tint32 = mscorlib.GetType ("System.Int32");
			Assert.That (ObjCGenerator.FormatRawValue (tint32, 1), Is.EqualTo ("1"), "Int32 1");
			var tuint32 = mscorlib.GetType ("System.UInt32");
			Assert.That (ObjCGenerator.FormatRawValue (tuint32, 1), Is.EqualTo ("1ul"), "UInt32 1");
			var tint64 = mscorlib.GetType ("System.Int64");
			Assert.That (ObjCGenerator.FormatRawValue (tint64, 1), Is.EqualTo ("1ll"), "Int64 1");
			var tuint64 = mscorlib.GetType ("System.UInt64");
			Assert.That (ObjCGenerator.FormatRawValue (tuint64, 1), Is.EqualTo ("1ull"), "UInt64 1");
		}
	}
}
