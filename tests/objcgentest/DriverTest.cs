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
				var embedder = new Embedder ();
				embedder.OutputDirectory = valid;
				Assert.That (Directory.Exists (valid), "valid");

				try {
					embedder.OutputDirectory = "/:output";
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
				var embedder = new Embedder ();
				embedder.SetTarget (t);
				Assert.That (embedder.TargetLanguage, Is.EqualTo (TargetLanguage.ObjectiveC), t);
			}
		}

		[Test]
		public void Target_NotSupported ()
		{
			var notsupported = new string [] { "C", "C++", "Java" };
			foreach (var t in notsupported) {
				try {
					var embedder = new Embedder ();
					embedder.SetTarget (t);
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
				var embedder = new Embedder ();
				embedder.SetTarget ("invalid");
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
				var embedder = new Embedder ();
				embedder.SetPlatform (p);
				Assert.That (embedder.Platform, Is.EqualTo (Platform.macOS), p);
			}

			foreach (var p in new string [] { "ios", "iOS" }) {
				var embedder = new Embedder ();
				embedder.SetPlatform (p);
				Assert.That (embedder.Platform, Is.EqualTo (Platform.iOS), p);
			}

			foreach (var p in new string [] { "tvos", "tvOS" }) {
				var embedder = new Embedder ();
				embedder.SetPlatform (p);
				Assert.That (embedder.Platform, Is.EqualTo (Platform.tvOS), p);
			}

			foreach (var p in new string [] { "watchos", "watchOS" }) {
				var embedder = new Embedder ();
				embedder.SetPlatform (p);
				Assert.That (embedder.Platform, Is.EqualTo (Platform.watchOS), p);
			}
		}

		[Test]
		public void Platform_NotSupported ()
		{
			var notsupported = new string [] { "Windows", "Android", "iOS", "tvOS", "watchOS" };
			foreach (var p in notsupported) {
				try {
					var embedder = new Embedder ();
					embedder.SetPlatform (p);
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
				var embedder = new Embedder ();
				embedder.SetPlatform ("invalid");
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

		[Test]
		public void CompilationTarget ()
		{
			var embedder = new Embedder ();
			Asserts.ThrowsEmbeddinatorException (5, "The compilation target `invalid` is not valid.", () => Driver.Main2 (new [] { "--target=invalid" }));
			Asserts.ThrowsEmbeddinatorException (5, "The compilation target `invalid` is not valid.", () => embedder.SetCompilationTarget ("invalid"));

			foreach (var ct in new string [] { "library", "sharedlibrary", "dylib" }) {
				embedder.SetCompilationTarget (ct);
				Assert.That (embedder.CompilationTarget, Is.EqualTo (global::Embeddinator.CompilationTarget.SharedLibrary), ct);
			}
			foreach (var ct in new string [] { "framework" }) {
				embedder.SetCompilationTarget (ct);
				Assert.That (embedder.CompilationTarget, Is.EqualTo (global::Embeddinator.CompilationTarget.Framework), ct);
			}
			foreach (var ct in new string [] { "static", "staticlibrary" }) {
				embedder.SetCompilationTarget (ct);
				Assert.That (embedder.CompilationTarget, Is.EqualTo (global::Embeddinator.CompilationTarget.StaticLibrary), ct);
			}
		}

		[Test]
		public void ABI ()
		{
			Driver.Main2 (new string [] { "--abi=armv7" });
			CollectionAssert.AreEquivalent (new string [] { "armv7" }, Driver.CurrentEmbedder.ABIs, "armv7");

			// We don't validate when setting the option
			Driver.Main2 (new string [] { "--abi=any" });
			CollectionAssert.AreEquivalent (new string [] { "any" }, Driver.CurrentEmbedder.ABIs, "any");
		}
	}
}