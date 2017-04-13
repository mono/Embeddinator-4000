using NUnit.Framework;
using System;
using ObjC;
using Embeddinator;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

namespace ObjCGeneratorTest {

	[TestFixture]
	public class ExtensionsTest {

		public static Universe Universe { get; } = new Universe (UniverseOptions.None);

		public static Assembly mscorlib { get; } = Universe.Load ("mscorlib.dll");

		[Test]
		public void Is ()
		{
			Assert.True (mscorlib.GetType ("System.Void").Is ("System", "Void"), "void");
			Assert.False (mscorlib.GetType ("System.Object").Is ("System", "Void"), "object");
		}

		[Test]
		public void HasAttribute ()
		{
			var fa = mscorlib.GetType ("System.IO.FileAccess");
			Assert.True (fa.HasCustomAttribute ("System", "FlagsAttribute"), "Has");
			Assert.False (fa.HasCustomAttribute ("System", "Flags"), "Has Not");
		}
	}
}
