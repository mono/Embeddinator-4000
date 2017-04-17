using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
		public static Embedder CurrentEmbedder { get; private set; }
		static int Main (string [] args)
		{
			try {
				return Main2 (args);
			} catch (Exception e) {
				ErrorHelper.Show (e);
				return 1;
			}
		}

		public static int Main2 (params string [] args)
		{
			var action = Action.None;
			var embedder = new Embedder ();

			CurrentEmbedder = embedder;

			var os = new OptionSet {
				{ "c|compile", "Compiles the generated output", v => embedder.CompileCode = true },
				{ "d|debug", "Build the native library with debug information.", v => embedder.Debug = true },
				{ "gen=", $"Target generator (default {embedder.TargetLanguage})", v => embedder.SetTarget (v) },
				{ "abi=", "A comma-separated list of ABIs to compile. If not specified, all ABIs applicable to the selected platform will be built. Valid values (also depends on platform): i386, x86_64, armv7, armv7s, armv7k, arm64.", (v) =>
					{
						embedder.ABIs.AddRange (v.Split (',').Select ((a) => a.ToLowerInvariant ()));
					}
				},
				{ "o|out|outdir=", "Output directory", v => embedder.OutputDirectory = v },
				{ "p|platform=", $"Target platform (iOS, macOS [default], watchOS, tvOS)", v => embedder.SetPlatform (v) },
				{ "dll|shared", "Compiles as a shared library (default)", v => embedder.CompilationTarget = CompilationTarget.SharedLibrary },
				{ "static", "Compiles as a static library (unsupported)", v => embedder.CompilationTarget = CompilationTarget.StaticLibrary },
				{ "vs=", $"Visual Studio version for compilation (unsupported)", v => { throw new EmbeddinatorException (2, $"Option `--vs` is not supported"); } },
				{ "h|?|help", "Displays the help", v => action = Action.Help },
				{ "v|verbose", "generates diagnostic verbose output", v => ErrorHelper.Verbosity++ },
				{ "version", "Display the version information.", v => action = Action.Version },
				{ "target=", "The compilation target (static, shared, framework).", v => embedder.SetCompilationTarget (v) },
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
					var result = embedder.Generate (assemblies);
					if (embedder.CompileCode && (result == 0))
						result = embedder.Compile ();
					Console.WriteLine ("Done");
					return result;
				} catch (NotImplementedException e) {
					throw new EmbeddinatorException (9, $"The feature `{e.Message}` is not currently supported by the tool");
				}
			default:
				throw ErrorHelper.CreateError (99, "Internal error: invalid action {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues)", action);
			}
		}
	}

	public class Embedder {

		string outputDirectory = ".";

		public string OutputDirectory {
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

		public bool CompileCode { get; set; }

		public bool Debug { get; set; }

		public bool Shared { get { return CompilationTarget == CompilationTarget.SharedLibrary; } }

		public string LibraryName { get; set; }

		public List<string> ABIs { get; private set; } = new List<string> ();

		public void SetPlatform (string platform)
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

		public void SetTarget (string value)
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

		public void SetCompilationTarget (string value)
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

		public Platform Platform { get; set; } = Platform.macOS;
		public TargetLanguage TargetLanguage { get; private set; } = TargetLanguage.ObjectiveC;
		public CompilationTarget CompilationTarget { get; set; } = CompilationTarget.SharedLibrary;

		public int Generate (List<string> args)
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

		static string xcode_app;
		static string XcodeApp {
			get {
				if (string.IsNullOrEmpty (xcode_app)) {
					int exitCode;
					string output;
					if (!RunProcess ("xcode-select", "-p", out exitCode, out output))
						throw ErrorHelper.CreateError (6, "Could not find the Xcode location.");
					xcode_app = Path.GetDirectoryName (Path.GetDirectoryName (output.Trim ()));
				}
				return xcode_app;
			}
		}

		class BuildInfo
		{
			public string Sdk;
			public string [] Architectures;
			public string SdkName; // used in -m{SdkName}-version-min
			public string MinVersion;
			public string XamariniOSSDK;
			public string CompilerFlags;
			public string LinkerFlags;
		}

		public int Compile ()
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
					new BuildInfo { Sdk = "AppleTVOS", Architectures = new string [] { "arm64" }, SdkName = "tvos", MinVersion = "9.0", XamariniOSSDK = "Xamarin.AppleTVOS.sdk", CompilerFlags = "-fembed-bitcode", LinkerFlags = "-fembed-bitcode" },
					new BuildInfo { Sdk = "AppleTVSimulator", Architectures = new string [] { "x86_64" }, SdkName = "tvos-simulator", MinVersion = "9.0", XamariniOSSDK = "Xamarin.AppleTVSimulator.sdk" },
				};
				break;
			case Platform.watchOS:
				build_infos = new BuildInfo [] {
					new BuildInfo { Sdk = "WatchOS", Architectures = new string [] { "armv7k" }, SdkName = "watchos", MinVersion = "2.0", XamariniOSSDK = "Xamarin.WatchOS.sdk", CompilerFlags = "-fembed-bitcode", LinkerFlags = "-fembed-bitcode"  },
					new BuildInfo { Sdk = "WatchSimulator", Architectures = new string [] { "i386" }, SdkName = "watchos-simulator", MinVersion = "2.0", XamariniOSSDK = "Xamarin.WatchSimulator.sdk" },
				};
				break;
			default:
				throw ErrorHelper.CreateError (99, "Internal error: invalid platform {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).", Platform);
			}

			// filter/validate ABIs
			if (ABIs.Count > 0) {
				// Validate
				var all_architectures = build_infos.SelectMany ((v) => v.Architectures);
				var invalid_architectures = ABIs.Except (all_architectures).ToArray ();
				if (invalid_architectures.Length > 0) {
					var arch = invalid_architectures [0];
					throw ErrorHelper.CreateError (8, $"The architecture '{arch}' is not valid for {Platform}. Valid architectures for {Platform} are: {string.Join (", ", all_architectures)}");
				}

				// Filter
				foreach (var info in build_infos)
					info.Architectures = info.Architectures.Where ((v) => ABIs.Contains (v)).ToArray ();
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
				Path.Combine (OutputDirectory, "objc-support.m"),
				Path.Combine (OutputDirectory, "bindings.m"),
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
					else
						common_options.Append ("-O2 -DTOKENLOOKUP ");
					common_options.Append ("-fobjc-arc ");
					common_options.Append ("-ObjC ");
					common_options.Append ("-Wall ");
					common_options.Append ($"-arch {arch} ");
					common_options.Append ($"-isysroot {XcodeApp}/Contents/Developer/Platforms/{build_info.Sdk}.platform/Developer/SDKs/{build_info.Sdk}.sdk ");
					common_options.Append ($"-m{build_info.SdkName}-version-min={build_info.MinVersion} ");
					common_options.Append ("-I/Library/Frameworks/Mono.framework/Versions/Current/include/mono-2.0 ");

					// Build each file to a .o
					var object_files = new List<string> ();
					foreach (var file in files) {
						var compiler_options = new StringBuilder (common_options.ToString ());
						compiler_options.Append ("-DMONO_EMBEDDINATOR_DLL_EXPORT ");
						compiler_options.Append (build_info.CompilerFlags).Append (" ");
						compiler_options.Append ("-c ");
						compiler_options.Append (Quote (file)).Append (" ");
						var objfile = Path.Combine (archOutputDirectory, Path.ChangeExtension (Path.GetFileName (file), "o"));
						compiler_options.Append ($"-o {Quote (objfile)} ");
						object_files.Add (objfile);
						if (!Xcrun (compiler_options, out exitCode))
							return exitCode;
					}

					switch (CompilationTarget) {
					case CompilationTarget.SharedLibrary:
						// Link all the .o files into a .dylib
						var options = new StringBuilder (common_options.ToString ());
						options.Append ($"-dynamiclib ");
						options.Append (build_info.LinkerFlags).Append (" ");
						options.Append ("-lobjc ");
						options.Append ("-framework CoreFoundation ");
						options.Append ("-framework Foundation ");
						options.Append ($"-install_name {Quote ("@rpath/" + output_file)} ");

						foreach (var objfile in object_files)
							options.Append (Quote (objfile)).Append (" ");

						var dynamic_ofile = Path.Combine (archOutputDirectory, output_file);
						options.Append ($"-o ").Append (Quote (dynamic_ofile)).Append (" ");
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
						// Archive all the .o files into a .a
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

			var output_path = Path.Combine (OutputDirectory, output_file);
			if (!Lipo (lipo_files, output_path, out exitCode))
				return exitCode;

			if (!DSymUtil (output_path, out exitCode))
				return exitCode;

			return 0;
		}

		string GetTargetFramework ()
		{
			switch (Platform) {
			case Platform.macOS:
				throw new NotImplementedException ("target framework for macOS");
			case Platform.iOS:
				return "Xamarin.iOS,v1.0";
			case Platform.tvOS:
				return "Xamarin.TVOS,v1.0";
			case Platform.watchOS:
				return "Xamarin.WatchOS,v1.0";
			default:
				throw ErrorHelper.CreateError (99, "Internal error: invalid platform {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).", Platform);
			}
		}
		
		public static bool RunProcess (string filename, string arguments, out int exitCode, out string stdout)
		{
			Console.WriteLine($"\t{filename} {arguments}");
			using (var p = new Process ()) {
				p.StartInfo.FileName = filename;
				p.StartInfo.Arguments = arguments;
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.RedirectStandardOutput = true;
				p.Start();
				stdout = p.StandardOutput.ReadToEnd();
				exitCode = p.ExitCode;
				return exitCode == 0;
			}
		}

		public static int RunProcess (string filename, string arguments)
		{
			Console.WriteLine ($"\t{filename} {arguments}");
			using (var p = Process.Start (filename, arguments)) {
				p.WaitForExit ();
				return p.ExitCode;
			}
		}

		public static bool RunProcess (string filename, string arguments, out int exitCode)
		{
			exitCode = RunProcess (filename, arguments);
			return exitCode == 0;
		}

		public static bool Xcrun (StringBuilder options, out int exitCode)
		{
			return RunProcess ("xcrun", options.ToString (), out exitCode);
		}

		bool DSymUtil (string input, out int exitCode)
		{
			exitCode = 0;

			if (!Debug)
				return true;

			string output;
			switch (CompilationTarget) {
			case CompilationTarget.StaticLibrary:
				return true;
			case CompilationTarget.SharedLibrary:
				output = input + ".dSYM";
				break;
			default:
				throw ErrorHelper.CreateError (99, "Internal error: invalid compilation target {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).", CompilationTarget);
			}

			var dsymutil_options = new StringBuilder ("dsymutil ");
			dsymutil_options.Append (Quote (input)).Append (" ");
			dsymutil_options.Append ($"-o {Quote (output)} ");
			return Xcrun (dsymutil_options, out exitCode);
		}

		public static bool Lipo (List<string> inputs, string output, out int exitCode)
		{
			Directory.CreateDirectory (Path.GetDirectoryName (output));
			if (inputs.Count == 1) {
				File.Copy (inputs [0], output, true);
				exitCode = 0;
				return true;
			} else {
				var lipo_options = new StringBuilder ("lipo ");
				foreach (var file in inputs)
					lipo_options.Append (file).Append (" ");
				lipo_options.Append ("-create -output ");
				lipo_options.Append (Quote (output));
				return Xcrun (lipo_options, out exitCode);
			}
		}

		public static string Quote (string f)
		{
			if (f.IndexOf (' ') == -1 && f.IndexOf ('\'') == -1 && f.IndexOf (',') == -1 && f.IndexOf ('$') == -1)
				return f;

			var s = new StringBuilder ();

			s.Append ('"');
			foreach (var c in f) {
				if (c == '"' || c == '\\')
					s.Append ('\\');

				s.Append (c);
			}
			s.Append ('"');

			return s.ToString ();
		}
	}
}
