using NUnit.Framework;
using System;
using Embeddinator.ObjC;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

namespace ObjCGeneratorTest {

	[TestFixture]
	public class TypeExtensionsTest {

		public static Universe Universe { get; } = new Universe (UniverseOptions.None);

		public static Assembly mscorlib { get; } = Universe.Load ("mscorlib.dll");

		[Test]
		public void Is ()
		{
			Assert.True (mscorlib.GetType ("System.Void").Is ("System", "Void"), "void");
			Assert.False (mscorlib.GetType ("System.Object").Is ("System", "Void"), "object");
		}

		[Test]
		public void HasCustomAttribute ()
		{
			var fa = mscorlib.GetType ("System.IO.FileAccess");
			Assert.True (fa.HasCustomAttribute ("System", "FlagsAttribute"), "Has");
			Assert.False (fa.HasCustomAttribute ("System", "Flags"), "Has Not");
		}

		[Test]
		public void Implements ()
		{
			Assert.True (mscorlib.GetType ("System.Int32").Implements ("System", "IComparable`1"), "int");
			Assert.False (mscorlib.GetType ("System.Object").Implements ("System", "IComparable"), "object");

			Assert.True (mscorlib.GetType ("System.IComparable").Implements ("System", "IComparable"), "IComparable");
			Assert.False (mscorlib.GetType ("System.IComparable").Implements ("System", "IComparable`1"), "IComparable`1");
		}

		[Test]
		public void Match ()
		{
			var o = mscorlib.GetType ("System.Object");
			var m1 = o.GetMethod ("GetHashCode");
			Assert.True (m1.Match ("System.Int32", "GetHashCode"), "GetHashCode");

			var m2 = o.GetMethod ("ReferenceEquals");
			Assert.False (m2.Match ("System.Boolean", "ReferenceEquals"), "ReferenceEquals - no param");
			Assert.True (m2.Match ("System.Boolean", "ReferenceEquals", "System.Object", "System.Object"), "Equals");
		}
	}
}
