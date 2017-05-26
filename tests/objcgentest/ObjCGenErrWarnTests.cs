using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

using NUnit.Framework;
using Embeddinator;
using DriverTest;

namespace ObjCGenErrWarnTests {

	public enum Configuration {
		Debug,
		Release
	}

	[TestFixture]
	public class GenErrWarnTests {
		[Test]
		[TestCase (
			// Warning message to [not] look for.
			arg1: new [] {
				"warning EM1011: Type `System.Type` is not generated because it lacks a native counterpart.",
			},
			// true: Does.Contain | false: Does.Not.Contain
			arg2: new [] {
				true,
			},
			// additional argument
			arg3: ""
		)]
		[TestCase (
			// Warning message to [not] look for.
			arg1: new [] {
				"warning EM1011: Type `System.Type` is not generated because it lacks a native counterpart.",
			},
			// true: Does.Contain | false: Does.Not.Contain
			arg2: new [] {
				false,
			},
			// additional argument
			arg3: "--nowarn:1011"
		)]
		// blocked by https://bugzilla.xamarin.com/show_bug.cgi?id=55801
		//[TestCase (
		//	// Warning message to [not] look for.
		//	arg1: new [] {
		//		"error EM1011: Type `System.Type` is not generated because it lacks a native counterpart.",
		//	},
		//	// true: Does.Contain | false: Does.Not.Contain
		//	arg2: new [] {
		//		false,
		//	},
		//	// additional argument
		//	arg3: "--warnaserror:1011"
		//)]
		public void GenWarningTest (string [] warnsToSearch, bool [] shouldFindWarn, string argument)
		{
			Assert.That (warnsToSearch.Length, Is.EqualTo (shouldFindWarn.Length), $"{nameof (warnsToSearch)} array length must match {nameof (shouldFindWarn)}'s");
			string tmpDir = Xamarin.Cache.CreateTemporaryDirectory ();
			var warnerrAssembly = ObjCGeneratorTest.Helpers.Universe.Load ("managedwarn");
			Assert.NotNull (warnerrAssembly, "warnerrAssembly");
			var stdout = Console.Out;
			var stderr = Console.Error;

			try {
				using (var ms = new MemoryStream ()) {
					var sw = new StreamWriter (ms);

					Console.SetOut (sw);
					Console.SetError (sw);

					try {
						var args = new List<string> { $"-o={tmpDir}", "-c", warnerrAssembly.Location };
						if (!String.IsNullOrWhiteSpace (argument))
							args.Add (argument);
					    Driver.Main2 (args.ToArray ());
					}
					catch (Exception) {
						throw;
					}
					sw.Flush ();
					ms.Position = 0;
					var sr = new StreamReader (ms);
					var output = sr.ReadToEnd ();

					var arrCount = warnsToSearch.Length;
					for (int i = 0; i < arrCount; i++) {
						if (shouldFindWarn [i])
							Assert.That (output, Does.Contain (warnsToSearch [i]), $"Did not find: {warnsToSearch [i]}");
						else
							Assert.That (output, Does.Not.Contain (warnsToSearch [i]), $"Did find: {warnsToSearch [i]}");
					}
				}
			} finally {
				Console.SetOut (stdout);
				Console.SetError (stderr);
			}
		}

		// We usually are in bin/debug|release
		static readonly string XcodeFolderPath = $"{AppDomain.CurrentDomain.BaseDirectory}../../xcode";
		static readonly string MonoMsBuildPath = "/Library/Frameworks/Mono.framework/Versions/Current/Commands/msbuild";
		static readonly string MonoPath = "/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono";

		[Test]
		[TestCase ("NoInitInSubclassTest", "ConstructorsLib", "main.m", Platform.macOS, Configuration.Debug, "error: 'initWithId:' is unavailable")]
		public void XcodeBuildErrorTest (string directoryTest, string csprojName, string objcmFileName, Platform platform, Configuration config, string errorToSearch)
		{
			Assert.IsTrue (Directory.Exists (XcodeFolderPath), "XcodeFolderPath");
			Assert.IsTrue (File.Exists (MonoMsBuildPath), "MonoMsBuildPath");
			Assert.IsTrue (File.Exists (MonoPath), "MonoPath");

			var testcaseBaseDir = Path.Combine (XcodeFolderPath, directoryTest);
			var tempWorkingDir = Xamarin.Cache.CreateTemporaryDirectory ();
			var e4kOutputDir = Path.Combine (tempWorkingDir, "E4KOutput");

			Asserts.RunProcess (MonoMsBuildPath, $"/p:Configuration={config} /p:IntermediateOutputPath={Path.Combine (tempWorkingDir, "obj")}/ /p:OutputPath={Path.Combine (tempWorkingDir, "DllOutput")} {Path.Combine (testcaseBaseDir, csprojName)}.csproj", "msbuildProc");

			var eargs = new List<string> {
					"-c",
					$"{Path.Combine (tempWorkingDir, "DllOutput", csprojName)}.dll",
					$"-o={e4kOutputDir}"
				};
			if (config == Configuration.Debug)
				eargs.Add ("--debug");

			Driver.Main2 (eargs.ToArray ());

			// Sadly no C# 7 yet
			// (string sdk, string arch, string sdkName, string minVersion) build_info;
			Tuple<string, string, string, string> build_info = null;

			switch (platform) {
			case Platform.macOS:
				build_info = new Tuple<string, string, string, string> ("MacOSX", "x86_64", "macosx", "10.7");
				break;
			case Platform.iOS:
				build_info = new Tuple<string, string, string, string> ("iPhoneSimulator", "x86_64", "ios-simulator", "8.0");
				break;
			case Platform.tvOS:
				build_info = new Tuple<string, string, string, string> ("AppleTVSimulator", "x86_64", "tvos-simulator", "9.0");
				break;
			case Platform.watchOS:
				build_info = new Tuple<string, string, string, string> ("WatchSimulator", "i386", "watchos-simulator", "2.0");
				break;
			}

			var clangArgs = new StringBuilder ("clang ");
			if (config == Configuration.Debug)
				clangArgs.Append ("-g -O0 ");
			else
				clangArgs.Append ("-O2 ");
			clangArgs.Append ("-fobjc-arc ");
			clangArgs.Append ("-ObjC ");
			clangArgs.Append ($"-arch {build_info.Item2} ");
			clangArgs.Append ($"-isysroot {Embedder.XcodeApp}/Contents/Developer/Platforms/{build_info.Item1}.platform/Developer/SDKs/{build_info.Item1}.sdk ");
			clangArgs.Append ($"-m{build_info.Item3}-version-min={build_info.Item4} ");
			clangArgs.Append ($"-I{e4kOutputDir} ");
			clangArgs.Append ($"-c {Path.Combine (testcaseBaseDir, objcmFileName)} ");
			clangArgs.Append ($"-o {Path.Combine (tempWorkingDir, "foo.o")} ");

			// Embedder.RunProcess returns false if exitcode != 0
			Assert.IsFalse (Utils.RunProcess ("xcrun", clangArgs.ToString (), out int exitCode, out string output, capture_stderr: true), "clangbuild");
			Assert.That (output, Does.Contain (errorToSearch), $"Not found: {errorToSearch}");
		}
	}
}
