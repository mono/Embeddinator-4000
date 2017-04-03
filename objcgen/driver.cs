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
			bool shared = true; // dylib
			var action = Action.None;

			var os = new OptionSet {
				{ "o|out|outdir=", "output directory", v => OutputDirectory = v },
				{ "h|?|help", "Displays the help", v => action = Action.Help },
				{ "v|verbose", "generates diagnostic verbose output", v => ErrorHelper.Verbosity++ },
				{ "version", "Display the version information.", v => action = Action.Version },
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
					return Generate (assemblies, shared);
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

		static int Generate (List<string> args, bool shared)
		{
			Console.WriteLine ("Parsing assemblies...");

			var universe = new Universe (UniverseOptions.MetadataOnly);
			var assemblies = new List<Assembly> ();
			foreach (var arg in args) {
				assemblies.Add (universe.LoadFile (arg));
				Console.WriteLine ($"\tParsed '{arg}'");
			}

			// by default the first specified assembly
			var name = Path.GetFileNameWithoutExtension (args [0]);

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
					if (shared || File.Exists ("main.c"))
						continue; 
				}
				var path = Path.Combine (OutputDirectory, res);
				Console.WriteLine ($"\tGenerated: {path}");
				using (var sw = new StreamWriter (path))
					exe.GetManifestResourceStream (res).CopyTo (sw.BaseStream);
			}

			StringBuilder options = new StringBuilder ("clang ");
			options.Append ("-DMONO_EMBEDDINATOR_DLL_EXPORT ");
			options.Append ("-framework CoreFoundation ");
			options.Append ("-framework Foundation ");
			options.Append ("-I\"/Library/Frameworks/Mono.framework/Versions/Current/include/mono-2.0\" -L\"/Library/Frameworks/Mono.framework/Versions/Current/lib/\" -lmonosgen-2.0 ");
			options.Append ("glib.c mono_embeddinator.c bindings.m ");
			if (shared)
				options.Append ($"-dynamiclib -install_name lib{name}.dylib ");
			else
				options.Append ("main.c ");
			options.Append ($"-o lib{name}.dylib -ObjC -lobjc");

			Console.WriteLine ("Compiling binding code...");
			Console.WriteLine ($"\tInvoking: xcrun {options}");
			var p = Process.Start ("xcrun", options.ToString ());
			p.WaitForExit ();
			Console.WriteLine ("Done");
			return p.ExitCode;
		}
	}
}
