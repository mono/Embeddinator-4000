using System;
using System.IO;

using NUnit.Framework;
using Embeddinator;

namespace DriverTest
{
	[TestFixture]
	public class EmbedderTest
	{
		[Test]
		[TestCase (Platform.iOS, "armv7k")]
		[TestCase (Platform.macOS, "armv7")]
		[TestCase (Platform.tvOS, "armv7")]
		[TestCase (Platform.watchOS, "arm64")]
		[TestCase (Platform.iOS, "invalid")]
		public void Invalid_ABI (Platform platform, string abi)
		{
			var dll = CompileLibrary (platform);
			string valid;
			switch (platform) {
			case Platform.iOS:
				valid = "armv7, armv7s, arm64, i386, x86_64";
				break;
			case Platform.macOS:
				valid = "i386, x86_64";
				break;
			case Platform.tvOS:
				valid = "arm64, x86_64";
				break;
			case Platform.watchOS:
				valid = "armv7k, i386";
				break;
			default:
				throw new NotImplementedException ();
			}
			Asserts.ThrowsEmbeddinatorException (8, $"The architecture '{abi}' is not valid for {platform}. Valid architectures for {platform} are: {valid}", () => Driver.Main2 ("--platform", platform.ToString (), "--abi", abi, "-c", dll, "-o", Xamarin.Cache.CreateTemporaryDirectory ()));
		}

		[Test]
		[TestCase (Platform.macOS, "x86_64")]
		public void ABI (Platform platform, string abi)
		{
			var dll = CompileLibrary (platform);
			var tmpdir = Xamarin.Cache.CreateTemporaryDirectory ();
			Driver.Main2 ("--platform", platform.ToString (), "--abi", abi, "-c", dll, "-o", tmpdir);

			string output;
			int exitCode;
			Assert.IsTrue (Embedder.RunProcess ("xcrun", $"lipo -info {tmpdir}/libLibrary.dylib", out exitCode, out output), "lipo");
			StringAssert.IsMatch ($"Non-fat file: .* is architecture: {abi}", output, "architecture");
		}

		string CompileLibrary (Platform platform, string code = null)
		{
			int exitCode;
			var tmpdir = Xamarin.Cache.CreateTemporaryDirectory ();
			var cs_path = Path.Combine (tmpdir, "library.cs");
			var dll_path = Path.Combine (tmpdir, "library.dll");

			if (code == null)
				code = "public class Test { public static void X () {} }";

			File.WriteAllText (cs_path, code);

			if (!Embedder.RunProcess ("/Library/Frameworks/Mono.framework/Versions/Current/bin/csc", $"/target:library {Embedder.Quote (cs_path)} /out:{Embedder.Quote (dll_path)}", out exitCode))
				Assert.Fail ("Failed to compile test code");

			return dll_path;
		}
	}
}
