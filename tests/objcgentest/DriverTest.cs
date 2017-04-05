using NUnit.Framework;
using System;
using System.IO;
using Embeddinator;

namespace DriverTest {

	[TestFixture]
	public class OptionsChecks {

		[Test]
		public void Output ()
		{
			Random rnd = new Random ();
			string valid = Path.Combine (Path.GetTempPath (), "output-" + rnd.Next ().ToString ());
			try {
				Driver.OutputDirectory = valid;
				Assert.That (Directory.Exists (valid), "valid");

				try {
					Driver.OutputDirectory = "/:output";
					Assert.Fail ("invalid");
				} catch (EmbeddinatorException ee) {
					Assert.True (ee.Error, "Error");
					Assert.That (ee.Code, Is.EqualTo (1), "Code");
				}
			} finally {
				Directory.Delete (valid);
			}
		}

		[Test]
		public void Target_Supported ()
		{
			var valid = new string [] { "ObjC", "Obj-C", "ObjectiveC", "objective-c" };
			foreach (var t in valid) {
				Driver.Target = t;
				Assert.That (Driver.Target, Is.EqualTo ("objc"), t);
			}
		}

		[Test]
		public void Target_NotSupported ()
		{
			var notsupported = new string [] { "C", "C++", "Java" };
			foreach (var t in notsupported) {
				try {
					Driver.Target = t;
				}
				catch (EmbeddinatorException ee) {
					Assert.True (ee.Error, "Error");
					Assert.That (ee.Code, Is.EqualTo (4), "Code");
				}
			}
		}

		[Test]
		public void Target_Invalid ()
		{
			try {
				Driver.Target = "invalid";
			}
			catch (EmbeddinatorException ee) {
				Assert.True (ee.Error, "Error");
				Assert.That (ee.Code, Is.EqualTo (4), "Code");
			}
		}

		[Test]
		public void Platform_Supported ()
		{
			var valid = new string [] { "OSX", "MacOSX", "MacOS", "Mac" };
			foreach (var p in valid) {
				Driver.Platform = p;
				Assert.That (Driver.Platform, Is.EqualTo ("macos"), p);
			}
		}

		[Test]
		public void Platform_NotSupported ()
		{
			var notsupported = new string [] { "Windows", "Android", "iOS", "tvOS", "watchOS" };
			foreach (var p in notsupported) {
				try {
					Driver.Platform = p;
				} catch (EmbeddinatorException ee) {
					Assert.True (ee.Error, "Error");
					Assert.That (ee.Code, Is.EqualTo (3), "Code");
				}
			}
		}

		[Test]
		public void Platform_Invalid ()
		{
			try {
				Driver.Platform = "invalid";
			} catch (EmbeddinatorException ee) {
				Assert.True (ee.Error, "Error");
				Assert.That (ee.Code, Is.EqualTo (3), "Code");
			}
		}

		[Test]
		public void Static_Unsupported ()
		{
			try {
				Driver.Main2 (new [] { "--static" });
			} catch (EmbeddinatorException ee) {
				Assert.True (ee.Error, "Error");
				Assert.That (ee.Code, Is.EqualTo (2), "Code");
			}
		}

		[Test]
		public void VS_Unsupported ()
		{
			try {
				Driver.Main2 (new [] { "--vs=x" });
			} catch (EmbeddinatorException ee) {
				Assert.True (ee.Error, "Error");
				Assert.That (ee.Code, Is.EqualTo (2), "Code");
			}
		}
	}
}