using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

using NUnit.Framework;
using Embeddinator;

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
			}
		)]
		public void GenWarningTest (string [] warnsToSearch, bool [] shouldFindWarn)
		{
			Assert.That (warnsToSearch.Length, Is.EqualTo (shouldFindWarn.Length), $"{nameof (warnsToSearch)} array length must match {nameof (shouldFindWarn)}'s");
			string valid = Xamarin.Cache.CreateTemporaryDirectory ();
			var warnerrAssembly = ObjCGeneratorTest.Helpers.Universe.Load ("managedwarn");
			Assert.NotNull (warnerrAssembly, "warnerrAssembly");

			try {
				using (var ms = new MemoryStream ()) {
					var stdout = Console.Out;
					var stderr = Console.Error;
					var sw = new StreamWriter (ms);

					Console.SetOut (sw);
					Console.SetError (sw);

					Driver.Main2 (new string [] { $"-o={valid}", "-c", warnerrAssembly.Location });
					sw.Flush ();
					ms.Position = 0;
					var sr = new StreamReader (ms);
					var output = sr.ReadToEnd ();

					Console.SetOut (stdout);
					Console.SetError (stderr);

					var arrCount = warnsToSearch.Length;
					for (int i = 0; i < arrCount; i++) {
						if (shouldFindWarn [i])
							Assert.That (output, Does.Contain (warnsToSearch [i]), $"Did not find: {warnsToSearch [i]}");
						else
							Assert.That (output, Does.Not.Contain (warnsToSearch [i]), $"Did find: {warnsToSearch [i]}");
					}
				}
			} finally {
				Directory.Delete (valid, true);
			}
		}

		// We usually are in bin/debug|release
		static readonly string XcodeFolderPath = $"{AppDomain.CurrentDomain.BaseDirectory}../../xcode";
		static readonly string MonoMsBuildPath = "/Library/Frameworks/Mono.framework/Versions/Current/Commands/msbuild";
		static readonly string MonoPath = "/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono";
		static readonly string XcodebuildPath = "/usr/bin/xcodebuild";

		[Test]
		[TestCase ("NoInitInSubclassTest", "ConstructorsLib", "NoInitInSubclassTest", "NoInitInSubclassTest", Configuration.Debug, "error: 'initWithId:' is unavailable", true)]
		public void XcodeBuildErrorTest (string directoryTest, string csprojName, string xcodeprojName, string xcodeprojTarget, Configuration config, string errorToSearch, bool cleanOutputDirs)
		{
			Assert.IsTrue (Directory.Exists (XcodeFolderPath), "XcodeFolderPath");
			Assert.IsTrue (File.Exists (MonoMsBuildPath), "MonoMsBuildPath");
			Assert.IsTrue (File.Exists (MonoPath), "MonoPath");
			Assert.IsTrue (File.Exists (XcodebuildPath), "XcodebuildPath");

			try {
				var testcaseBaseDir = Path.Combine (XcodeFolderPath, directoryTest);
				var msbuildInfo = new ProcessStartInfo {
					FileName = MonoMsBuildPath,
					Arguments = $"/p:Configuration={config} /p:OutputPath=DllOutput {Path.Combine (testcaseBaseDir, csprojName)}.csproj"
				};

				using (var msbuildProc = Process.Start (msbuildInfo)) {
					msbuildProc.WaitForExit ();
					Assert.That (msbuildProc.ExitCode, Is.Zero, "msbuildProc");
				}

				var eargs = new List<string> {
					"-c",
					$"{Path.Combine (testcaseBaseDir, "DllOutput", csprojName)}.dll",
					$"-o={Path.Combine (testcaseBaseDir, "E4KOutput")}"
				};
				if (config == Configuration.Debug)
					eargs.Add ("--debug");

				Driver.Main2 (eargs.ToArray ());

				var xbuildInfo = new ProcessStartInfo {
					FileName = XcodebuildPath,
					Arguments = $"-project {Path.Combine (testcaseBaseDir, xcodeprojName)}.xcodeproj -target {xcodeprojTarget} -configuration {config}",
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardError = true,
					RedirectStandardOutput = true
				};

				using (var xcodebuildProc = Process.Start (xbuildInfo)) {
					xcodebuildProc.WaitForExit ();
					Assert.That (xcodebuildProc.ExitCode, Is.GreaterThanOrEqualTo (1), "xcodebuildProc");
					var stdout = xcodebuildProc.StandardOutput.ReadToEnd ();
					Assert.That (stdout, Does.Contain (errorToSearch), $"Not Found: {errorToSearch}");
				}

			} finally {
				if (cleanOutputDirs) {
					var dllOutputDir = Path.Combine (XcodeFolderPath, directoryTest, "DllOutput");
					var objDir = Path.Combine (XcodeFolderPath, directoryTest, "obj");
					var ek4OutputDir = Path.Combine (XcodeFolderPath, directoryTest, "E4KOutput");
					var buildDir = Path.Combine (XcodeFolderPath, directoryTest, "build");
					if (Directory.Exists (dllOutputDir))
						Directory.Delete (dllOutputDir, true);
					if (Directory.Exists (objDir))
						Directory.Delete (objDir, true);
					if (Directory.Exists (ek4OutputDir))
						Directory.Delete (ek4OutputDir, true);
					if (Directory.Exists (buildDir))
						Directory.Delete (buildDir, true);
				}
			}
		}
	}
}
