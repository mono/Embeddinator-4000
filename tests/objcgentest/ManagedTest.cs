﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

using Embeddinator.ObjC;

using Xamarin;
using DriverTest;

using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace ExecutionTests
{
	[TestFixture]
	public class ManagedTest
	{
		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void macOS (bool debug)
		{
			RunManagedTests (Platform.macOS, debug: debug);
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void macOSModern (bool debug)
		{
			RunManagedTests (Platform.macOSModern, debug: debug);
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void FSharp_macOSModern (bool debug)
		{
			RunManagedTests (new ManagedTestInfo (Platform.macOSModern, ManagedTestVariant.FSharp), debug: debug);
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void ManagedException_macOSModern (bool debug)
		{
			RunManagedTests (new ManagedTestInfo (Platform.macOSModern, ManagedTestVariant.ManagedExceptions), debug: debug, single_test: "Tests/Tests/testExceptions");
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void macOSSystem (bool debug)
		{
			RunManagedTests (Platform.macOSSystem, debug: debug);
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void macOSFull (bool debug)
		{
			RunManagedTests (Platform.macOSFull, debug: debug);
		}

		[Test]
		public void macOS_Extension_With_Spaces ()
		{
			RunManagedTests (Platform.macOSModern, debug: true, forceSpaces: true, additionalArgs: "--extension");
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void iOS_simulator (bool debug)
		{
			RunManagedTests (Platform.iOS, "-destination 'platform=iOS Simulator,name=iPhone 6,OS=latest'", debug: debug);
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void iOS_device (bool debug)
		{
			if (string.IsNullOrEmpty (iOSDeviceIdentifier))
				Assert.Ignore ("No applicable iOS device connected.");
			
			RunManagedTests (Platform.iOS, $"-destination 'platform=iOS,id={iOSDeviceIdentifier}'", debug: debug);
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void tvOS_simulator (bool debug)
		{
			RunManagedTests (Platform.tvOS, "-destination 'platform=tvOS Simulator,name=Apple TV 4K (at 1080p),OS=latest'", debug: debug);
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void tvOS_device (bool debug)
		{
			if (string.IsNullOrEmpty (tvOSDeviceIdentifier))
				Assert.Ignore ("No applicable tvOS device connected.");

			RunManagedTests (Platform.tvOS, $"-destination 'platform=tvOS,id={tvOSDeviceIdentifier}'", debug: debug);
		}

		XmlDocument device_xml;
		string FindDevice (params string [] valid_classes)
		{
			if (device_xml == null) {
				var cachedir = Cache.CreateTemporaryDirectory ();
				var xmlpath = Path.Combine (cachedir, "devices.xml");
				Asserts.RunProcess ("/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/bin/mlaunch", $"--listdev={Utils.Quote (xmlpath)} --output-format=xml", "mlaunch --listdev");
				var settings = new XmlReaderSettings () {
 					XmlResolver = null,
 					DtdProcessing = DtdProcessing.Parse
 				};
				device_xml = new XmlDocument ();
				var sr = new StreamReader (xmlpath, Encoding.UTF8, true);
				var reader = XmlReader.Create (sr, settings);
				device_xml.Load (reader);
			}
			// filter by device type and ensure that we can use the device for debugging purposes.
			var xpathQuery = "/MTouch/Device[(" + string.Join(" or ", valid_classes.Select((v) => "DeviceClass = \"" + v + "\"")) + ") and IsUsableForDebugging =\"True\"]/DeviceIdentifier";
			var nodes = device_xml.SelectNodes (xpathQuery);
			var devices = new List<string> ();
			foreach (XmlNode node in nodes)
				devices.Add (node.InnerText);
			if (devices.Count == 0)
				return string.Empty;
			
			devices.Sort (StringComparer.Ordinal); // Sort all applicable devices so the same test run always runs on the same device.
			return devices [0];
		}

		// A device identifier for an iOS device. Can be either iPhone or iPad. Will be an empty string if no applicable devices are attached.
		string ios_device_identifier;
		string iOSDeviceIdentifier {
			get {
				if (ios_device_identifier == null)
					ios_device_identifier = FindDevice ("iPhone", "iPad");
				return ios_device_identifier;
			}
		}

		// A device identifier for a tvOS device. Will be an empty string if no applicable devices are attached.
		string tvos_device_identifier;
		string tvOSDeviceIdentifier {
			get {
				if (tvos_device_identifier == null)
					tvos_device_identifier = FindDevice ("AppleTV");
				return tvos_device_identifier;
			}
		}

		enum ManagedTestVariant { None, FSharp, ManagedExceptions }

		class ManagedTestInfo
		{
			public Platform Platform { get; }
			public string Dllname { get; }
			public string Dlldir { get; }
			public string Abi { get; }
			public List<string> Defines { get; } = new List<string> ();
			public int ManagedTestCount { get; }
			public string AdditionalGeneratorArgs { get; } = null;

			public ManagedTestInfo (Platform platform, ManagedTestVariant type = ManagedTestVariant.None)
			{
				bool fsharp = type == ManagedTestVariant.FSharp;
				Platform = platform;

				ManagedTestCount = GetBaseTestCount (fsharp);
				Defines.Add ("TEST_FRAMEWORK=1");

				// There would be a lot of fsharp projects to clone to do them all...
				if (fsharp && Platform != Platform.macOSModern)
					throw new NotImplementedException ();

				if (type == ManagedTestVariant.ManagedExceptions) {
					Defines.Add ("NATIVEEXCEPTION");
					AdditionalGeneratorArgs = "--nativeexception";
				}

				switch (platform) {
				case Platform.macOSFull:
					Dlldir = "macos-full";
					Dllname = "managed-macos-full.dll";
					Defines.Add ("XAMARIN_MAC=1");
					Defines.Add ("XAMARIN_MAC_FULL=1");
					Abi = "x86_64"; // FIXME: fat XM apps not supported yet
					ManagedTestCount += GetMacTestCount ();
					return;
				case Platform.macOSSystem:
					Dlldir = "macos-system";
					Dllname = "managed-macos-system.dll";
					Defines.Add ("XAMARIN_MAC=1");
					Defines.Add ("XAMARIN_MAC_SYSTEM=1");
					Abi = "x86_64"; // FIXME: fat XM apps not supported yet
					ManagedTestCount += GetMacTestCount ();
					return;
				case Platform.macOSModern:
					if (fsharp) {
						Dlldir = "fsharp-macos";
						Dllname = "fsharp-macos.dll";
						Defines.Add ("XAMARIN_FSHARP=1");
					} else {
						Dlldir = "macos-modern";
						Dllname = "managed-macos-modern.dll";
						Defines.Add ("XAMARIN_MAC_MODERN=1");
					}
					Defines.Add ("XAMARIN_MAC=1");
					Abi = "x86_64"; // FIXME: fat XM apps not supported yet
					ManagedTestCount += GetMacTestCount (fsharp);
					return;
				case Platform.macOS:
					Dlldir = "generic";
					Dllname = "managed.dll";
					Abi = "x86_64";
					return;
				case Platform.iOS:
					Dlldir = "ios";
					Dllname = "managed-ios.dll";
					Defines.Add ("XAMARIN_IOS=1");
					Abi = "armv7,arm64,i386,x86_64";
					ManagedTestCount += GetIOSTestCount ();
					return;
				case Platform.tvOS:
					Dlldir = "tvos";
					Dllname = "managed-tvos.dll";
					Defines.Add ("XAMARIN_TVOS=1");
					Abi = "arm64,x86_64";
					ManagedTestCount += GetTVTestCount ();
					return;
				default:
					throw new NotImplementedException ();
				}
			}

			int GetBaseTestCount (bool fsharp = false) => CountTests (Path.Combine (XcodeProjectGenerator.TestsRootDirectory, "objc-cli/libmanaged/Tests/Tests.m"), fsharp);
			int GetIOSTestCount () => CountTests (Path.Combine (XcodeProjectGenerator.TestsRootDirectory, "objcgentest/xcodetemplate/ios/test/iosTests.m"));
			int GetMacTestCount (bool fsharp = false) => CountTests (Path.Combine (XcodeProjectGenerator.TestsRootDirectory, "objcgentest/xcodetemplate/macos/test/macTests.m"), fsharp);
			int GetTVTestCount () => CountTests (Path.Combine (XcodeProjectGenerator.TestsRootDirectory, "objcgentest/xcodetemplate/tvos/test/tvosTests.m"));

			const string TestRegex = "^\\s*-\\s*[(]\\s*void\\s*[)]\\s*test";

			int CountTests (string path, bool fsharp = false) => File.ReadAllLines (path).Count ((v) => (v.Contains ("fsharp") == fsharp) && System.Text.RegularExpressions.Regex.IsMatch (v, TestRegex));
		}


		void RunManagedTests (Platform platform, string test_destination = "", bool debug = true, string single_test = null, bool forceSpaces = false, string additionalArgs = "")
		{
			RunManagedTests (new ManagedTestInfo (platform), test_destination, debug, single_test, forceSpaces, additionalArgs);
		}

		void RunManagedTests (ManagedTestInfo test, string test_destination = "", bool debug = true, string single_test = null, bool forceSpaces = false, string additionalArgs = "")
		{
			var tmpdir = Cache.CreateTemporaryDirectory ();
			var configuration = debug ? "Debug" : "Release";
			var dll_path = Path.Combine (XcodeProjectGenerator.TestsRootDirectory, "managed", test.Dlldir, "bin", configuration, test.Dllname);

			// This will build all the managed.dll variants, which is easier than calculating the relative path _as the makefile sees it_ to pass as the target.
			Asserts.RunProcess ("make", $"all CONFIG={configuration} -C {Utils.Quote (Path.Combine (XcodeProjectGenerator.TestsRootDirectory, "managed"))}", "build " + Path.GetFileName (dll_path));

			if (forceSpaces) {
				string dll_folder = Path.GetDirectoryName (dll_path);
				Directory.CreateDirectory (Path.Combine (dll_folder, "with spaces"));
				string dll_path_with_spaces = Path.Combine (dll_folder, "with spaces", Path.GetFileName (dll_path));
				File.Copy (dll_path, dll_path_with_spaces, true);
				dll_path = dll_path_with_spaces;
			}

			var outdir = tmpdir + "/out";
			var projectName = "foo";
			var args = new List<string> ();
			if (debug)
				args.Add ("--debug");
			args.Add (dll_path);
			args.Add ("-c");
			args.Add ($"--outdir={outdir}");
			args.Add ("--target=framework");
			args.Add ($"--platform={test.Platform}");
			args.Add ($"--abi={test.Abi}");
			if (test.AdditionalGeneratorArgs != null)
				args.Add (test.AdditionalGeneratorArgs);

			Asserts.Generate ("generate", args.ToArray ());

			var framework_path = Path.Combine (outdir, Path.GetFileNameWithoutExtension (dll_path) + ".framework");
			var projectDirectory = XcodeProjectGenerator.Generate (test.Platform, tmpdir, projectName, framework_path, defines: test.Defines.ToArray ());

			string output;
			var builddir = Path.Combine (tmpdir, "xcode-build-dir");
			string testName = single_test != null ? $"-only-testing:{single_test}" : "";
			Asserts.RunProcess ("xcodebuild", $"test -project {Utils.Quote (projectDirectory)} -scheme Tests {test_destination} {testName} CONFIGURATION_BUILD_DIR={Utils.Quote (builddir)}", out output, "run xcode tests");
			// assert the number of tests passed, so that we can be sure we actually ran all the tests. Otherwise it's very easy to ignore when a bug is causing tests to not be built.
			if (single_test == null)
				Assert.That (output, Does.Match ($"Test Suite 'All tests' passed at .*\n\t Executed {test.ManagedTestCount} tests, with 0 failures"), "test count");
			else
				Assert.That (output, Does.Match ($"Test Suite 'Selected tests' passed at .*\n\t Executed 1 test, with 0 failures"), "test count");
		}
	}

	public static class XcodeProjectGenerator
	{
		public static string Generate (Platform platform, string outputDirectory, string projectName, string framework_reference_path, string [] defines = null)
		{
			switch (platform) {
			case Platform.macOS:
			case Platform.macOSFull:
			case Platform.macOSModern:
			case Platform.macOSSystem:
				return Generate ("macos", outputDirectory, projectName, framework_reference_path, defines);
			case Platform.iOS:
				return Generate ("ios", outputDirectory, projectName, framework_reference_path, defines);
			case Platform.tvOS:
				return Generate ("tvos", outputDirectory, projectName, framework_reference_path, defines);
			default:
				throw new NotImplementedException ();
			}
		}

		static string Generate (string infix, string outputDirectory, string projectName, string framework_reference_path, string [] defines)
		{
			var projectDirectory = Path.Combine (outputDirectory, $"{projectName}.xcodeproj");
			var sourceDirectory = Path.Combine (outputDirectory, projectName);
			var testDirectory = Path.Combine (outputDirectory, "Tests");
			var asm = typeof (XcodeProjectGenerator).Assembly;

			Directory.CreateDirectory (projectDirectory);

			var prefixes = new [] {
				new { Prefix = $"objcgentest.xcodetemplate.{infix}.src.", Directory = sourceDirectory },
				new { Prefix = $"objcgentest.xcodetemplate.{infix}.proj.", Directory = projectDirectory },
				new { Prefix = $"objcgentest.xcodetemplate.{infix}.test.", Directory = testDirectory },
			};
			foreach (var res in asm.GetManifestResourceNames ()) {
				foreach (var prefix in prefixes) {
					if (!res.StartsWith (prefix.Prefix, StringComparison.Ordinal))
						continue;
					var relative_path = res.Substring (prefix.Prefix.Length);
					var full_path = Path.Combine (prefix.Directory, relative_path);
					Directory.CreateDirectory (Path.GetDirectoryName (full_path));
					using (var sw = new StreamWriter (full_path)) {
						using (var str = asm.GetManifestResourceStream (res))
							str.CopyTo (sw.BaseStream);
					}
					ProcessFile (full_path, projectName, framework_reference_path: framework_reference_path, defines: defines);
					break;
				}
			}
			return projectDirectory;
		}

		static void ProcessFile (string filename, string project_name, string framework_reference_path = null, string dylib_reference_path = null, string [] defines = null)
		{
			var contents = File.ReadAllText (filename);
			contents = contents.Replace ("%TESTS_ROOT_DIR%", Path.GetFullPath (TestsRootDirectory));
			contents = contents.Replace ("%PROJECT_NAME%", project_name);
			if (!string.IsNullOrEmpty (framework_reference_path)) {
				contents = contents.Replace ("%FRAMEWORK_REFERENCE_NAME%", Path.GetFileNameWithoutExtension (framework_reference_path));
				contents = contents.Replace ("%FRAMEWORK_REFERENCE_DIR%", Path.GetFullPath (Path.GetDirectoryName (framework_reference_path)));
			}
			if (defines?.Length > 0) {
				contents = contents.Replace ("%GCC_PREPROCESSOR_DEFINITIONS%", string.Join ("\n\t\t\t\t\t\t\t", defines.Select ((v) => "\"" + v + "\",")));
			} else {
				contents = contents.Replace ("%GCC_PREPROCESSOR_DEFINITIONS%", "");
			}

			File.WriteAllText (filename, contents);
		}

		public static string TestsRootDirectory {
			get {
				var dir = Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly ().Location);
				while (dir.Length > 1 && Path.GetFileName (dir) != "tests")
					dir = Path.GetDirectoryName (dir);
				return dir;
			}
		}
	}
}
