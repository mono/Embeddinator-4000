using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using IKVM.Reflection;
using Type = IKVM.Reflection.Type;
using System.Text;

using Mono.Options;

using ObjC;

namespace Embeddinator {

	enum Action
	{
		None,
		Help,
		Version,
		Generate,
	}

	public enum Platform
	{
		macOS,
		iOS,
		watchOS,
		tvOS,
	}

	public enum CompilationTarget
	{
		SharedLibrary,
		StaticLibrary,
		Framework,
	}

	public enum TargetLanguage
	{
		ObjectiveC,
	}

	public static class Driver
	{
		static int Main (string [] args)
		{
			try {
				return Main2 (args);
			} catch (Exception e) {
				ErrorHelper.Show (e);
				return 1;
			}
		}

		public static int Main2 (string [] args)
		{
			var action = Action.None;

			var os = new OptionSet {
				{ "c|compile", "Compiles the generated output", v => CompileCode = true },
				{ "d|debug", "Build the native library with debug information.", v => Debug = true },
				{ "gen=", $"Target generator (default {TargetLanguage})", v => SetTarget (v) },
				{ "o|out|outdir=", "Output directory", v => OutputDirectory = v },
				{ "p|platform=", $"Target platform (iOS, macOS [default], watchOS, tvOS)", v => SetPlatform (v) },
				{ "dll|shared", "Compiles as a shared library (default)", v => CompilationTarget = CompilationTarget.SharedLibrary },
				{ "static", "Compiles as a static library (unsupported)", v => CompilationTarget = CompilationTarget.StaticLibrary },
				{ "vs=", $"Visual Studio version for compilation (unsupported)", v => { throw new EmbeddinatorException (2, $"Option `--vs` is not supported"); } },
				{ "h|?|help", "Displays the help", v => action = Action.Help },
				{ "v|verbose", "generates diagnostic verbose output", v => ErrorHelper.Verbosity++ },
				{ "version", "Display the version information.", v => action = Action.Version },
				{ "target=", "The compilation target (static, shared, framework).", v => SetCompilationTarget (v) },
			};

			var assemblies = os.Parse (args);

			if (action == Action.None && assemblies.Count > 0)
				action = Action.Generate;

			switch (action) {
			case Action.None:
			case Action.Help:
				Console.WriteLine ($"Embeddinator-4000 v0.1 ({Info.Branch}: {Info.Hash})");
				Console.WriteLine ("Generates target language bindings for interop with managed code.");
				Console.WriteLine ("");
				Console.WriteLine ($"Usage: {Path.GetFileName (System.Reflection.Assembly.GetExecutingAssembly ().Location)} [options]+ ManagedAssembly1.dll [ManagedAssembly2.dll ...]");
				Console.WriteLine ();
				os.WriteOptionDescriptions (Console.Out);
				return 0;
			case Action.Version:
				Console.WriteLine ($"Embeddinator-4000 v0.1 ({Info.Branch}: {Info.Hash})");
				return 0;
			case Action.Generate:
				try {
					var result = Generate (assemblies);
					if (CompileCode && (result == 0))
						result = Compile ();
					Console.WriteLine ("Done");
					return result;
				} catch (NotImplementedException e) {
					throw new EmbeddinatorException (1000, $"The feature `{e.Message}` is not currently supported by the tool");
				}
			default:
				throw ErrorHelper.CreateError (99, "Internal error: invalid action {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues)", action);
			}
		}

		static string outputDirectory = ".";

		public static string OutputDirectory {
			get { return outputDirectory; }
			set {
				if (!Directory.Exists (value)) {
					try {
						Directory.CreateDirectory (value);
						Console.WriteLine ($"Creating output directory `{Path.GetFullPath (value)}`");
					} catch (Exception e) {
						throw new EmbeddinatorException (1, true, e, $"Could not create output directory `{value}`");
					}
				}
				outputDirectory = value;
			}
		}

		static bool CompileCode { get; set; }

		static bool Debug { get; set; }

		static bool Shared { get { return CompilationTarget == CompilationTarget.SharedLibrary; } }

		static string LibraryName { get; set; }

		public static void SetPlatform (string platform)
		{
			switch (platform.ToLowerInvariant ()) {
			case "osx":
			case "macosx":
			case "macos":
			case "mac":
				Platform = Platform.macOS;
				break;
			case "ios":
				Platform = Platform.iOS;
				break;
			case "tvos":
				Platform = Platform.tvOS;
				break;
			case "watchos":
				Platform = Platform.watchOS;
				break;
			default:
				throw new EmbeddinatorException (3, true, $"The platform `{platform}` is not valid.");
			}
		}

		public static void SetTarget (string value)
		{
			switch (value.ToLowerInvariant ()) {
			case "objc":
			case "obj-c":
			case "objectivec":
			case "objective-c":
				TargetLanguage = TargetLanguage.ObjectiveC;
				break;
			default:
				throw new EmbeddinatorException (4, true, $"The target `{value}` is not valid.");
			}
		}

		public static void SetCompilationTarget (string value)
		{
			switch (value.ToLowerInvariant ()) {
			case "library":
			case "sharedlibrary":
			case "dylib":
				CompilationTarget = CompilationTarget.SharedLibrary;
				break;
			case "framework":
				CompilationTarget = CompilationTarget.Framework;
				break;
			case "static":
			case "staticlibrary":
				CompilationTarget = CompilationTarget.StaticLibrary;
				break;
			default:
				throw new EmbeddinatorException (5, true, $"The compilation target `{value}` is not valid.");
			}
		}

		public static Platform Platform { get; set; } = Platform.macOS;
		public static TargetLanguage TargetLanguage { get; private set; } = TargetLanguage.ObjectiveC;
		public static CompilationTarget CompilationTarget { get; set; } = CompilationTarget.SharedLibrary;

		static int Generate (List<string> args)
		{
			Console.WriteLine ("Parsing assemblies...");

			var universe = new Universe (UniverseOptions.MetadataOnly);
			var assemblies = new List<Assembly> ();
			foreach (var arg in args) {
				assemblies.Add (universe.LoadFile (arg));
				Console.WriteLine ($"\tParsed '{arg}'");
			}

			// if not specified then we use the first specified assembly name
			if (LibraryName == null)
				LibraryName = Path.GetFileNameWithoutExtension (args [0]);

			Console.WriteLine ("Processing assemblies...");
			var g = new ObjCGenerator ();
			g.Process (assemblies);

			Console.WriteLine ("Generating binding code...");
			g.Generate (assemblies);
			g.Write (OutputDirectory);

			var exe = typeof (Driver).Assembly;
			foreach (var res in exe.GetManifestResourceNames ()) {
				if (res == "main.c") {
					// no main is needed for dylib and don't re-write an existing main.c file - it's a template
					if (CompilationTarget != CompilationTarget.StaticLibrary || File.Exists ("main.c"))
						continue;
				}
				var path = Path.Combine (OutputDirectory, res);
				Console.WriteLine ($"\tGenerated: {path}");
				using (var sw = new StreamWriter (path))
					exe.GetManifestResourceStream (res).CopyTo (sw.BaseStream);
			}
			return 0;
		}

		class BuildInfo
		{
			public string Sdk;
			public string [] Architectures;
			public string SdkName; // used in -m{SdkName}-version-min
			public string MinVersion;
			public string XamariniOSSDK;
		}

		static int Compile ()
		{
			Console.WriteLine ("Compiling binding code...");

			BuildInfo [] build_infos;

			switch (Platform) {
			case Platform.macOS:
				build_infos = new BuildInfo [] {
					new BuildInfo { Sdk = "MacOSX", Architectures = new string [] { "i386", "x86_64" }, SdkName = "macosx", MinVersion = "10.7" },
				};
				break;
			case Platform.iOS:
				build_infos = new BuildInfo [] {
					new BuildInfo { Sdk = "iPhoneOS", Architectures = new string [] { "armv7", "armv7s", "arm64" }, SdkName = "iphoneos", MinVersion = "8.0", XamariniOSSDK = "MonoTouch.iphoneos.sdk" },
					new BuildInfo { Sdk = "iPhoneSimulator", Architectures = new string [] { "i386", "x86_64" }, SdkName = "ios-simulator", MinVersion = "8.0", XamariniOSSDK = "MonoTouch.iphonesimulator.sdk" },
				};
				break;
			case Platform.tvOS:
				build_infos = new BuildInfo [] {
					new BuildInfo { Sdk = "AppleTVOS", Architectures = new string [] { "arm64" }, SdkName = "tvos", MinVersion = "9.0", XamariniOSSDK = "Xamarin.AppleTVOS.sdk" },
					new BuildInfo { Sdk = "AppleTVSimulator", Architectures = new string [] { "x86_64" }, SdkName = "tvos-simulator", MinVersion = "9.0", XamariniOSSDK = "Xamarin.AppleTVSimulator.sdk" },
				};
				break;
			case Platform.watchOS:
				build_infos = new BuildInfo [] {
					new BuildInfo { Sdk = "WatchOS", Architectures = new string [] { "armv7k" }, SdkName = "watchos", MinVersion = "2.0", XamariniOSSDK = "Xamarin.WatchOS.sdk" },
					new BuildInfo { Sdk = "WatchSimulator", Architectures = new string [] { "i386" }, SdkName = "watchos-simulator", MinVersion = "2.0", XamariniOSSDK = "Xamarin.WatchSimulator.sdk" },
				};
				break;
			default:
				throw ErrorHelper.CreateError (99, "Internal error: invalid platform {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).", Platform);
			}

			switch (CompilationTarget) {
			case CompilationTarget.SharedLibrary:
			case CompilationTarget.StaticLibrary:
				break;
			case CompilationTarget.Framework:
				throw new NotImplementedException ($"Compilation target: {CompilationTarget}");
			default:
				throw ErrorHelper.CreateError (99, "Internal error: invalid compilation target {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).", CompilationTarget);
			}


			var lipo_files = new List<string> ();
			var output_file = string.Empty;

			var files = new string [] {
				Path.Combine (OutputDirectory, "glib.c"),
				Path.Combine (OutputDirectory, "mono_embeddinator.c"),
				Path.Combine (OutputDirectory, "bindings.m")
			};

			switch (CompilationTarget) {
			case CompilationTarget.SharedLibrary:
				output_file = $"lib{LibraryName}.dylib";
				break;
			case CompilationTarget.StaticLibrary:
				output_file = $"{LibraryName}.a";
				break;
			default:
				throw ErrorHelper.CreateError (99, "Internal error: invalid compilation target {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).", CompilationTarget);
			}

			int exitCode;

			foreach (var build_info in build_infos) {
				foreach (var arch in build_info.Architectures) {
					var archOutputDirectory = Path.Combine (OutputDirectory, arch);
					Directory.CreateDirectory (archOutputDirectory);

					var common_options = new StringBuilder ("clang ");
					if (Debug)
						common_options.Append ("-g -O0 ");
					common_options.Append ("-fobjc-arc ");
					common_options.Append ("-ObjC ");
					common_options.Append ($"-arch {arch} ");
					common_options.Append ($"-isysroot /Applications/Xcode.app/Contents/Developer/Platforms/{build_info.Sdk}.platform/Developer/SDKs/{build_info.Sdk}.sdk ");
					common_options.Append ($"-m{build_info.SdkName}-version-min={build_info.MinVersion} ");

					// Build each file to a .o
					var object_files = new List<string> ();
					foreach (var file in files) {
						var static_options = new StringBuilder (common_options.ToString ());
						static_options.Append ("-I/Library/Frameworks/Mono.framework/Versions/Current/include/mono-2.0 ");
						static_options.Append ("-DMONO_EMBEDDINATOR_DLL_EXPORT ");
						static_options.Append ("-c ");
						static_options.Append (file).Append (" ");
						var objfile = Path.Combine (archOutputDirectory, Path.ChangeExtension (Path.GetFileName (file), "o"));
						static_options.Append ($"-o {objfile} ");
						object_files.Add (objfile);
						if (!Xcrun (static_options, out exitCode))
							return exitCode;
					}

					switch (CompilationTarget) {
					case CompilationTarget.SharedLibrary:
						var options = new StringBuilder (common_options.ToString ());
						options.Append ($"-dynamiclib ");
						options.Append ("-lobjc ");
						options.Append ("-framework CoreFoundation ");
						options.Append ("-framework Foundation ");
						options.Append ($"-install_name @rpath/{output_file} ");

						foreach (var objfile in object_files)
							options.Append (objfile).Append (" ");
						
						var dynamic_ofile = Path.Combine (archOutputDirectory, output_file);
						options.Append ($"-o ").Append (dynamic_ofile).Append (" ");
						lipo_files.Add (dynamic_ofile);

						if (!string.IsNullOrEmpty (build_info.XamariniOSSDK)) {
							options.Append ($"-L/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/SDKs/{build_info.XamariniOSSDK}/usr/lib ");
						} else {
							options.Append ("-L/Library/Frameworks/Mono.framework/Versions/Current/lib/ ");
						}
						options.Append ("-lmonosgen-2.0 ");
						if (!Xcrun (options, out exitCode))
							return exitCode;
						break;
					case CompilationTarget.StaticLibrary:
						// Collect all the .o files into a .a
						var archive_options = new StringBuilder ("ar cru ");
						var static_ofile = Path.Combine (archOutputDirectory, output_file);
						archive_options.Append (static_ofile).Append (" ");
						lipo_files.Add (static_ofile);
						foreach (var objfile in object_files)
							archive_options.Append (objfile).Append (" ");
						if (!Xcrun (archive_options, out exitCode))
							return exitCode;
						break;
					case CompilationTarget.Framework:
						throw new NotImplementedException ("compile to framework");
					default:
						throw ErrorHelper.CreateError (99, "Internal error: invalid compilation target {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).", CompilationTarget);
					}
				}
			}

			if (lipo_files.Count == 1) {
				File.Copy (lipo_files [0], Path.Combine (OutputDirectory, output_file), true);
			} else {
				var lipo_options = new StringBuilder ("lipo ");
				foreach (var file in lipo_files)
					lipo_options.Append (file).Append (" ");
				lipo_options.Append ("-create -output ");
				lipo_options.Append (Path.Combine (OutputDirectory, output_file));
				var rv = RunProcess ("xcrun", lipo_options.ToString ());
				if (rv != 0)
					return rv;
			}

			if (Debug && CompilationTarget != CompilationTarget.StaticLibrary) {
				var dsymutil_options = new StringBuilder ("dsymutil ");
				dsymutil_options.Append (Path.Combine (OutputDirectory, output_file));
				var rv = RunProcess ("xcrun", dsymutil_options.ToString ());
				if (rv != 0)
					return rv;
			}

			return 0;
		}

		static int RunProcess (string filename, string arguments)
		{
			Console.WriteLine ($"\t{filename} {arguments}");
			using (var p = Process.Start (filename, arguments)) {
				p.WaitForExit ();
				return p.ExitCode;
			}
		}

		static bool Xcrun (StringBuilder options, out int exitCode)
		{
			exitCode = RunProcess ("xcrun", options.ToString ());
			return exitCode == 0;
		}
	}
}
