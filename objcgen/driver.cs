using System;
using System.Linq;

using Mono.Options;

namespace Embeddinator.ObjC
{
	enum Action
	{
		None,
		Help,
		Version,
		Generate,
	}

	public enum Platform
	{
		macOS, // Does not use Xamarin.Mac
		macOSFull,
		macOSModern,
		macOSSystem,
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
				{ "e|extension", "Compiles the generated output as extension safe api", v => embedder.Extension = true },
				{ "bitcode=", "Compiles the generated output with bitcode (default, true, false)", v => embedder.SetBitcode (v) },
				{ "d|debug", "Build the native library with debug information.", v => embedder.Debug = true },
				{ "gen=", $"Target generator (default {embedder.TargetLanguage})", v => embedder.SetTarget (v) },
				{ "abi=", "A comma-separated list of ABIs to compile. If not specified, all ABIs applicable to the selected platform will be built. Valid values (also depends on platform): i386, x86_64, armv7, armv7s, armv7k, arm64.", (v) =>
					{
						embedder.ABIs.AddRange (v.Split (',').Select ((a) => a.ToLowerInvariant ()));
					}
				},
				{ "o|out|outdir=", "Output directory", v => embedder.OutputDirectory = v },
				{ "p|platform=", $"Target platform (iOS, macOS [default], macos-[modern|full|system], watchOS, tvOS)", v => embedder.SetPlatform (v) },
				{ "vs=", $"Visual Studio version for compilation (unsupported)", v => { throw new EmbeddinatorException (2, $"Option `--vs` is not supported"); } },
				{ "h|?|help", "Displays the help", v => action = Action.Help },
				{ "v|verbose", "generates diagnostic verbose output", v => ErrorHelper.Verbosity++ },
				{ "version", "Display the version information.", v => action = Action.Version },
				{ "target=", "The compilation target (staticlibrary, sharedlibrary, framework).", embedder.SetCompilationTarget },
				{ "warnaserror:", "An optional comma-separated list of warning codes that should be reported as errors (if no warnings are specified all warnings are reported as errors).", v => {
					try {
						if (!string.IsNullOrEmpty (v)) {
							foreach (var code in v.Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries))
								ErrorHelper.SetWarningLevel (ErrorHelper.WarningLevel.Error, int.Parse (code));
						} else {
							ErrorHelper.SetWarningLevel (ErrorHelper.WarningLevel.Error);
						}
					} catch (Exception ex) {
						ErrorHelper.Error (26, ex, "Could not parse the command line argument '{0}': {1}", "--warnaserror", ex.Message);
					}
				}},
				{ "nowarn:", "An optional comma-separated list of warning codes to ignore (if no warnings are specified all warnings are ignored).", v => {
					try {
						if (!string.IsNullOrEmpty (v)) {
							foreach (var code in v.Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries))
								ErrorHelper.SetWarningLevel (ErrorHelper.WarningLevel.Disable, int.Parse (code));
						} else {
							ErrorHelper.SetWarningLevel (ErrorHelper.WarningLevel.Disable);
						}
					} catch (Exception ex) {
						ErrorHelper.Error (26, ex, "Could not parse the command line argument '{0}': {1}", "--nowarn", ex.Message);
					}
				}},
			};

			var assemblies = os.Parse (args);

			if (action == Action.None && assemblies.Count > 0)
				action = Action.Generate;

			switch (action) {
			case Action.None:
			case Action.Help:
				Console.WriteLine ($"Embeddinator-4000 {Info.Version} ({Info.Branch}: {Info.Hash})");
				Console.WriteLine ("Generates target language bindings for interop with managed code.");
				Console.WriteLine ("");
				Console.WriteLine ($"Usage: objcgen [options]+ ManagedAssembly1.dll [ManagedAssembly2.dll ...]");
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
					throw new EmbeddinatorException (9, true, e, $"The feature `{e.Message}` is not currently supported by the tool");
				}
			default:
				throw ErrorHelper.CreateError (99, "Internal error: invalid action {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues)", action);
			}
		}
	}
}
