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

		static int Compile ()
		{
			Console.WriteLine ("Compiling binding code...");

			switch (Platform) {
			case Platform.macOS:
				break;
			case Platform.iOS:
			case Platform.watchOS:
			case Platform.tvOS:
				throw new NotImplementedException ($"platform={Platform}");
			default:
				throw ErrorHelper.CreateError (99, "Internal error: invalid platform {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).", Platform);
			}

			switch (CompilationTarget) {
			case CompilationTarget.SharedLibrary:
				break;
			case CompilationTarget.Framework:
			case CompilationTarget.StaticLibrary:
				throw new NotImplementedException ($"Compilation target: {CompilationTarget}");
			default:
				throw ErrorHelper.CreateError (99, "Internal error: invalid compilation target {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).", CompilationTarget);
			}

			StringBuilder options = new StringBuilder ("clang ");
			if (Debug)
				options.Append ("-g -O0 ");
			options.Append ("-fobjc-arc ");
			options.Append ("-DMONO_EMBEDDINATOR_DLL_EXPORT ");
			options.Append ("-framework CoreFoundation ");
			options.Append ("-framework Foundation ");
			options.Append ("-I\"/Library/Frameworks/Mono.framework/Versions/Current/include/mono-2.0\" -L\"/Library/Frameworks/Mono.framework/Versions/Current/lib/\" -lmonosgen-2.0 ");
			options.Append ("glib.c mono_embeddinator.c bindings.m ");
			options.Append ("-ObjC -lobjc");
			switch (CompilationTarget) {
			case CompilationTarget.SharedLibrary:
				options.Append ($"-dynamiclib ");
				options.Append ($"-install_name @rpath/lib{LibraryName}.dylib ");
				options.Append ($"-o lib{LibraryName}.dylib ");
				break;
			case CompilationTarget.StaticLibrary:
				throw new NotImplementedException ("compile to static library");
			case CompilationTarget.Framework:
				throw new NotImplementedException ("compile to framework");
			default:
				throw ErrorHelper.CreateError (99, "Internal error: invalid compilation target {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).", CompilationTarget);
			}

			Console.WriteLine ("Compiling binding code...");
			Console.WriteLine ($"\tInvoking: xcrun {options}");
			var p = Process.Start ("xcrun", options.ToString ());
			p.WaitForExit ();
			return p.ExitCode;
		}
	}
}
