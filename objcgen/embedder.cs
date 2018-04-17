using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using IKVM.Reflection;
using System.Text;
using System.Xml;

namespace Embeddinator.ObjC
{
	public class Embedder
	{
		string outputDirectory = ".";

		public string OutputDirectory
		{
			get { return outputDirectory; }
			set
			{
				if (!Directory.Exists (value)) {
					try {
						Directory.CreateDirectory (value);
						Console.WriteLine ($"Creating output directory `{Path.GetFullPath (value)}`");
					}
					catch (Exception e) {
						throw new EmbeddinatorException (1, true, e, $"Could not create output directory `{value}`");
					}
				}
				outputDirectory = value;
			}
		}

		string bundleIdentifier;
		public string BundleIdentifier
		{
			get { return bundleIdentifier ?? LibraryName; }
			set { bundleIdentifier = value; }
		}

		public bool CompileCode { get; set; }

		public bool Debug { get; set; }

		public bool Extension { get; set; }

		public bool? BitcodeOption { get; set; }

		public bool Shared { get { return CompilationTarget == CompilationTarget.SharedLibrary; } }

		public bool EnableLinker { get; set; }

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
					EnableLinker = false;
					break;
				case "macosmodern":
				case "macos-modern":
					Platform = Platform.macOSModern;
					EnableLinker = true;
					break;
				case "macosfull":
				case "macos-full":
					Platform = Platform.macOSFull;
					EnableLinker = false;
					break;
				case "macossystem":
				case "macos-system":
					Platform = Platform.macOSSystem;
					EnableLinker = false;
					break;
				case "ios":
					Platform = Platform.iOS;
					EnableLinker = true;
					break;
				case "tvos":
					Platform = Platform.tvOS;
					EnableLinker = true;
					break;
				case "watchos":
					Platform = Platform.watchOS;
					EnableLinker = true;
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

		public void SetBitcode (string value)
		{
			switch (value.ToLowerInvariant ())
			{
				case "default":
					BitcodeOption = null;
					break;
				case "true":
					BitcodeOption = true;
					break;
				case "false":
					BitcodeOption = false;
					break;
				default:
					throw new EmbeddinatorException (17, true, $"The bitcode option `{value}` is not valid.");
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

		public List<Assembly> Assemblies { get; private set; } = new List<Assembly> ();
		public Platform Platform { get; set; } = Platform.macOS;
		public TargetLanguage TargetLanguage { get; private set; } = TargetLanguage.ObjectiveC;
		public CompilationTarget CompilationTarget { get; set; } = CompilationTarget.SharedLibrary;

		public string [] PlatformSdkDirectories
		{
			get
			{
				switch (Platform) {
					case Platform.iOS:
						return new[] { "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/Xamarin.iOS" };
					case Platform.tvOS:
						return new[] { "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/Xamarin.TVOS" };
					case Platform.watchOS:
						return new[] { "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/Xamarin.WatchOS" };
					case Platform.macOS:
					case Platform.macOSSystem:
						return new[] { "/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.5", "/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/lib/mono/4.5" };
					case Platform.macOSFull:
						return new[] { "/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/lib/mono/4.5" };
					case Platform.macOSModern:
						return new[] { "/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/lib/mono/Xamarin.Mac" };
					default:
						throw ErrorHelper.CreateError (99, "Internal error: invalid platform {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).", Platform);
				}
			}
		}

		public int Generate (List<string> args)
		{
			Console.WriteLine ("Parsing assemblies...");

			var universe = new Universe (UniverseOptions.MetadataOnly);

			universe.AssemblyResolve += (object sender, IKVM.Reflection.ResolveEventArgs resolve_args) => {
				var directories = new List<string> ();
				foreach (var v in PlatformSdkDirectories) {
					directories.Add (v);
					directories.Add (Path.Combine (v, "Facades"));
				}
				foreach (var asm in Assemblies)
					directories.Add (Path.GetDirectoryName (asm.Location));

				AssemblyName an = new AssemblyName (resolve_args.Name);
				foreach (var dir in directories) {
					var filename = Path.Combine (dir, an.Name + ".dll");
					if (File.Exists (filename))
						return universe.LoadFile (filename);
				}
				throw ErrorHelper.CreateError (13, $"Can't find the assembly '{resolve_args.Name}', referenced by '{resolve_args.RequestingAssembly.FullName}'.");
			};

			foreach (var arg in args) {
				if (!File.Exists (arg))
					throw ErrorHelper.CreateError (11, $"The assembly {arg} does not exist.");

				Assemblies.Add (universe.LoadFile (arg));
				Console.WriteLine ($"\tParsed '{arg}'");
			}

			// if not specified then we use the first specified assembly name
			if (LibraryName == null)
				LibraryName = Path.GetFileNameWithoutExtension (args[0]);

			Console.WriteLine ("Processing assemblies...");
			var p = new ObjCProcessor ();
			p.Process (Assemblies.Select (x => new ProcessedAssembly (x) { UserCode = true}));

			Console.WriteLine ("Generating binding code...");
			var g = new ObjCGenerator () {
				Processor = p,
				GenerateLinkerExclusions = EnableLinker,
			};
			g.Generate ();
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
		public static string XcodeApp
		{
			get
			{
				if (string.IsNullOrEmpty (xcode_app)) {
					int exitCode;
					string output;
					if (!Utils.RunProcess ("xcode-select", "-p", out exitCode, out output))
						throw ErrorHelper.CreateError (6, "Could not find the Xcode location.");
					xcode_app = Path.GetDirectoryName (Path.GetDirectoryName (output.Trim ()));
				}
				return xcode_app;
			}
		}

		class BuildInfo
		{
			public string Sdk;
			public string[] Architectures;
			public string SdkName; // used in -m{SdkName}-version-min
			public string MinVersion;
			public string XamariniOSSDK;
			public string CompilerFlags;
			public string LinkerFlags;

			public bool IsSimulator
			{
				get { return Sdk.Contains ("Simulator"); }
			}
		}

		void VerifyDependencies ()
		{
			SystemCheck.VerifyMono ();
			switch (Platform) {
				case Platform.iOS:
				case Platform.tvOS:
				case Platform.watchOS:
					SystemCheck.VerifyXamariniOS ();
					break;
				case Platform.macOS:
				case Platform.macOSFull:
				case Platform.macOSModern:
				case Platform.macOSSystem:
					SystemCheck.VerifyXamarinMac ();
					break;
				default:
					throw ErrorHelper.CreateError (99, "Internal error: invalid platform {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).", Platform);
			}
		}

		string GetBitcodeFlag (bool defaultsToEnable)
		{
			const string option = "-fembed-bitcode";
			if (BitcodeOption == null)
				return defaultsToEnable ? option : string.Empty;
			else
				return  BitcodeOption == true ? option : string.Empty;
		}

		public int Compile ()
		{
			Console.WriteLine ("Compiling binding code...");

			VerifyDependencies ();

			BuildInfo[] build_infos;

			switch (Platform) {
				case Platform.macOS:
				case Platform.macOSFull:
				case Platform.macOSModern:
				case Platform.macOSSystem:
					build_infos = new BuildInfo[] {
					new BuildInfo { Sdk = "MacOSX", Architectures = new string [] { "i386", "x86_64" }, SdkName = "macosx", MinVersion = "10.7" },
				};
					break;
				case Platform.iOS:
					build_infos = new BuildInfo[] {
					new BuildInfo { Sdk = "iPhoneOS", Architectures = new string [] { "armv7", "armv7s", "arm64" }, SdkName = "iphoneos", MinVersion = "8.0", XamariniOSSDK = "MonoTouch.iphoneos.sdk", CompilerFlags = GetBitcodeFlag(defaultsToEnable: false), LinkerFlags = GetBitcodeFlag(defaultsToEnable: false) },
					new BuildInfo { Sdk = "iPhoneSimulator", Architectures = new string [] { "i386", "x86_64" }, SdkName = "ios-simulator", MinVersion = "8.0", XamariniOSSDK = "MonoTouch.iphonesimulator.sdk" },
				};
					break;
				case Platform.tvOS:
					build_infos = new BuildInfo[] {
					new BuildInfo { Sdk = "AppleTVOS", Architectures = new string [] { "arm64" }, SdkName = "tvos", MinVersion = "9.0", XamariniOSSDK = "Xamarin.AppleTVOS.sdk", CompilerFlags = GetBitcodeFlag(defaultsToEnable: true), LinkerFlags = GetBitcodeFlag(defaultsToEnable: true) },
					new BuildInfo { Sdk = "AppleTVSimulator", Architectures = new string [] { "x86_64" }, SdkName = "tvos-simulator", MinVersion = "9.0", XamariniOSSDK = "Xamarin.AppleTVSimulator.sdk" },
				};
					break;
				case Platform.watchOS:
					build_infos = new BuildInfo[] {
					new BuildInfo { Sdk = "WatchOS", Architectures = new string [] { "armv7k" }, SdkName = "watchos", MinVersion = "2.0", XamariniOSSDK = "Xamarin.WatchOS.sdk", CompilerFlags = GetBitcodeFlag(defaultsToEnable: true), LinkerFlags = GetBitcodeFlag(defaultsToEnable: true) },
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
					var arch = invalid_architectures[0];
					throw ErrorHelper.CreateError (8, $"The architecture '{arch}' is not valid for {Platform}. Valid architectures for {Platform} are: {string.Join (", ", all_architectures)}");
				}

				// Filter
				foreach (var info in build_infos)
					info.Architectures = info.Architectures.Where ((v) => ABIs.Contains (v)).ToArray ();
			}

			var lipo_files = new List<string> ();
			var output_file = string.Empty;

			var files = new string[] {
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
				case CompilationTarget.Framework:
					output_file = $"{LibraryName}.a";
					break;
				default:
					throw ErrorHelper.CreateError (99, "Internal error: invalid compilation target {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).", CompilationTarget);
			}

			int exitCode;

			/*
			 * For static libraries we
			 * * Compile all source files to .o files, per architecture.
			 * * Then we archive .o files into per-sdk .a files, then we archive all .o files into a global .a file. So we end up with one .a for device, one for simulator, and one for both device and simulator.
			 * * Then we dsym the global .a file
			 * 
			 * For dynamic libraries we
			 * * Compile all source files to .o files, per architecture.
			 * * Then we link all .o files per architecture into a .dylib.
			 * * Then we lipo .dylib files into per-sdk fat .dylib, then we lipo all .dylib into a global .dylib file. So we end up with one fat .dylib for device, one fat .dylib for simulator, and one very fat .dylib for both simulator and device.
			 * * Finally we dsymutil the global .dylib
			 * 
			 * For frameworks we
			 * * First we build the source files to .o files and then archive to .a files just like for static libraries.
			 * * Then we call mtouch to build a framework of the managed assemblies, passing the static library we just created as a gcc_flag so that it's linked in. This is done per sdk (simulator/device).
			 * * Finally we merge the simulator framework and the device framework into a global framework, that supports both simulator and device.
			 * * We dsymutil those frameworks.
			 */

			foreach (var build_info in build_infos) {
				foreach (var arch in build_info.Architectures) {
					var archOutputDirectory = Path.Combine (OutputDirectory, arch);
					Directory.CreateDirectory (archOutputDirectory);

					var common_options = new StringBuilder ("clang ");
					if (Debug)
						common_options.Append ("-g -O0 ");
					else {
						common_options.Append ("-O2 ");
						if (Platform == Platform.macOS) {
							// Token lookup only works if the linker isn't involved.
							// If the linker is enabled, all assemblies are loaded and re-saved
							// (even if only linking SDK assemblies), which means metadata
							// tokens may change even for non-linked assemblies. So completely 
							// disable token lookup for platforms that uses the linker (all platforms
							// except macOS).
							common_options.Append ("-DTOKENLOOKUP ");
						}
					}

					if (Extension)
						common_options.Append ("-fapplication-extension ");

					common_options.Append ("-fobjc-arc ");
					common_options.Append ("-ObjC ");
					common_options.Append ("-Wall ");
					common_options.Append ($"-arch {arch} ");
					common_options.Append ($"-isysroot {XcodeApp}/Contents/Developer/Platforms/{build_info.Sdk}.platform/Developer/SDKs/{build_info.Sdk}.sdk ");
					common_options.Append ($"-m{build_info.SdkName}-version-min={build_info.MinVersion} ");

					switch (Platform) {
						case Platform.macOS:
							common_options.Append ("-I/Library/Frameworks/Mono.framework/Versions/Current/include/mono-2.0 ");
							break;
						case Platform.macOSSystem:
						case Platform.macOSModern:
						case Platform.macOSFull:
							common_options.Append ("-I/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/include ");
							common_options.Append ("-DXAMARIN_MAC ");
							break;
						case Platform.iOS:
						case Platform.tvOS:
						case Platform.watchOS:
							common_options.Append ($"-I/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/SDKs/{build_info.XamariniOSSDK}/usr/include ");
							common_options.Append ("-DXAMARIN_IOS ");
							break;
						default:
							throw ErrorHelper.CreateError (99, "Internal error: invalid platform {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).", Platform);
					}

					// Build each source file to a .o
					var object_files = new List<string> ();
					foreach (var file in files) {
						var compiler_options = new StringBuilder (common_options.ToString ());
						compiler_options.Append ("-DMONO_EMBEDDINATOR_DLL_EXPORT ");
						compiler_options.Append (build_info.CompilerFlags).Append (" ");
						compiler_options.Append ("-c ");
						compiler_options.Append (Utils.Quote (file)).Append (" ");
						var objfile = Path.Combine (archOutputDirectory, Path.ChangeExtension (Path.GetFileName (file), "o"));
						compiler_options.Append ($"-o {Utils.Quote (objfile)} ");
						object_files.Add (objfile);
						if (!Utils.Xcrun (compiler_options, out exitCode))
							return exitCode;
					}

					// Link/archive object files to .a/.dylib
					switch (CompilationTarget) {
						case CompilationTarget.SharedLibrary:
							// Link all the .o files into a .dylib
							var options = new StringBuilder (common_options.ToString ());
							options.Append ($"-dynamiclib ");
							options.Append (build_info.LinkerFlags).Append (" ");
							options.Append ("-lobjc ");
							options.Append ("-framework CoreFoundation ");
							options.Append ("-framework Foundation ");
							options.Append ($"-install_name {Utils.Quote ("@rpath/" + output_file)} ");

							foreach (var objfile in object_files)
								options.Append (Utils.Quote (objfile)).Append (" ");

							var dynamic_ofile = Path.Combine (archOutputDirectory, output_file);
							options.Append ($"-o ").Append (Utils.Quote (dynamic_ofile)).Append (" ");
							lipo_files.Add (dynamic_ofile);
							if (!string.IsNullOrEmpty (build_info.XamariniOSSDK)) {
								options.Append ($"-L/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/SDKs/{build_info.XamariniOSSDK}/usr/lib ");
								options.Append ("-lxamarin ");
							}
							else {
								options.Append ("-L/Library/Frameworks/Mono.framework/Versions/Current/lib/ ");
							}
							options.Append ("-lmonosgen-2.0 ");
							if (!Utils.Xcrun (options, out exitCode))
								return exitCode;
							break;
						case CompilationTarget.Framework:
						case CompilationTarget.StaticLibrary:
							// Archive all the .o files into a .a
							var archive_options = new StringBuilder ("ar cru ");
							var static_ofile = Path.Combine (archOutputDirectory, output_file);
							archive_options.Append (Utils.Quote (static_ofile)).Append (" ");
							lipo_files.Add (static_ofile);
							foreach (var objfile in object_files)
								archive_options.Append (objfile).Append (" ");
							if (!Utils.Xcrun (archive_options, out exitCode))
								return exitCode;
							break;
						default:
							throw ErrorHelper.CreateError (99, "Internal error: invalid compilation target {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).", CompilationTarget);
					}
				}

				// Create the per-sdk output file
				var sdk_output_file = Path.Combine (OutputDirectory, build_info.Sdk, output_file);
				if (!Utils.Lipo (lipo_files, sdk_output_file, out exitCode))
					return exitCode;

				if (CompilationTarget == CompilationTarget.Framework) {
					var appdir = Path.GetFullPath (Path.Combine (OutputDirectory, build_info.Sdk, LibraryName));
					var cachedir = Path.GetFullPath (Path.Combine (outputDirectory, build_info.Sdk, "build-cache"));

					string fwdir;
					string headers;

					switch (Platform) {
						case Platform.macOS:
						case Platform.macOSFull:
						case Platform.macOSModern:
						case Platform.macOSSystem:
							fwdir = Path.Combine (appdir, $"{LibraryName}.framework");
							headers = Path.Combine (fwdir, "Headers");
							Directory.CreateDirectory (Path.Combine (fwdir, "Versions", "A", "Headers"));
							Utils.CreateSymlink (Path.Combine (fwdir, "Versions", "Current"), "A");
							Utils.CreateSymlink (Path.Combine (fwdir, "Headers"), "Versions/Current/Headers");
							Utils.CreateSymlink (Path.Combine (fwdir, "Resources"), "Versions/Current/Resources");
							break;
						case Platform.iOS:
						case Platform.tvOS:
						case Platform.watchOS:
							fwdir = Path.Combine (appdir, "Frameworks", $"{LibraryName}.framework");
							headers = Path.Combine (fwdir, "Headers");
							Directory.CreateDirectory (headers);
							break;
						default:
							throw ErrorHelper.CreateError (99, "Internal error: invalid platform {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).", Platform);
					}

					switch (Platform) {
						case Platform.macOS:
							var resources_dir = Path.Combine (fwdir, "Versions", "A", "Resources");
							Directory.CreateDirectory (resources_dir);

							// Link the .a files into a framework
							var options = new StringBuilder ("clang ");
							options.Append ($"-dynamiclib ");
							options.Append (build_info.LinkerFlags).Append (" ");
							options.Append ("-lobjc ");
							options.Append ("-framework CoreFoundation ");
							options.Append ("-framework Foundation ");
							options.Append ($"-install_name {Utils.Quote ($"@loader_path/../Frameworks/{LibraryName}.framework/Versions/A/{LibraryName}")} ");
							options.Append ("-force_load ").Append (Utils.Quote (sdk_output_file)).Append (" ");
							options.Append ($"-o ").Append (Utils.Quote (Path.Combine (fwdir, "Versions", "A", LibraryName))).Append (" ");
							options.Append ("-L/Library/Frameworks/Mono.framework/Versions/Current/lib/ ");
							options.Append ("-lmonosgen-2.0 ");
							if (!Utils.Xcrun (options, out exitCode))
								return exitCode;
							// Create framework structure
							Utils.CreateSymlink (Path.Combine (fwdir, LibraryName), $"Versions/Current/{LibraryName}");

							File.WriteAllText (Path.Combine (fwdir, "Versions", "A", "Resources", "Info.plist"), $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
        <key>CFBundleDevelopmentRegion</key>
        <string>English</string>
        <key>CFBundleExecutable</key>
        <string>{LibraryName}</string>
        <key>CFBundleIdentifier</key>
        <string>{BundleIdentifier}</string>
        <key>CFBundleInfoDictionaryVersion</key>
        <string>6.0</string>
        <key>CFBundleName</key>
        <string>{LibraryName}</string>
        <key>CFBundlePackageType</key>
        <string>FMWK</string>
        <key>CFBundleShortVersionString</key>
        <string>1.0</string>
        <key>CFBundleSignature</key>
        <string>????</string>
        <key>CFBundleSupportedPlatforms</key>
        <array>
                <string>MacOSX</string>
        </array>
        <key>CFBundleVersion</key>
        <string>1</string>
        <key>DTCompiler</key>
        <string>com.apple.compilers.llvm.clang.1_0</string>
        <key>DTPlatformBuild</key>
        <string>8C38</string>
        <key>DTPlatformVersion</key>
        <string>GM</string>
        <key>DTSDKBuild</key>
        <string>16C58</string>
        <key>DTSDKName</key>
        <string>macosx10.12</string>
        <key>DTXcode</key>
        <string>0620</string>
        <key>DTXcodeBuild</key>
        <string>6C131e</string>
        <key>BuildMachineOSBuild</key>
        <string>13F34</string>
</dict>
</plist>
");

							// Copy any assemblies to the framework
							var bundleDir = Path.Combine (resources_dir, "MonoBundle");
							Directory.CreateDirectory (bundleDir);
							foreach (var asm in Assemblies) {
								var src = asm.Location;
								var tgt = Path.Combine (bundleDir, Path.GetFileName (asm.Location));
								File.Copy (src, tgt, true);
								Utils.FileCopyIfExists (Path.ChangeExtension (src, "pdb"), Path.ChangeExtension (tgt, "pdb"));
								Utils.FileCopyIfExists (src + ".config", tgt + ".config");
								// FIXME: Satellite assemblies?
							}
							// Add the headers to the framework
							File.Copy (Path.Combine (OutputDirectory, "embeddinator.h"), Path.Combine (headers, "embeddinator.h"), true);
							File.Copy (Path.Combine (OutputDirectory, "bindings.h"), Path.Combine (headers, $"bindings.h"), true);
							// Create an umbrella header for the everything in the framework.
							File.WriteAllText (Path.Combine (headers, LibraryName + ".h"),
	@"
#include ""bindings.h""
");
							break;
						case Platform.macOSFull:
						case Platform.macOSModern:
						case Platform.macOSSystem:
							var mmp = new StringBuilder ();
							mmp.Append ($"--output={Utils.Quote (appdir)} ");
							mmp.Append ($"--arch={string.Join (",", build_info.Architectures)} ");
							if (build_info.Architectures.Length > 1)
								throw new NotImplementedException ("fat Xamarin.Mac apps");
							mmp.Append ($"--sdkroot {XcodeApp} ");
							mmp.Append ($"--minos {build_info.MinVersion} ");
							mmp.Append ($"--embeddinator ");
							foreach (var asm in Assemblies)
								mmp.Append (Utils.Quote (Path.GetFullPath (asm.Location))).Append (" ");
							mmp.Append ($"-a:{GetPlatformAssembly ()} ");
							mmp.Append ($"--sdk {GetSdkVersion (build_info.Sdk.ToLower ())} ");
							// FIXME: once merged add support for linking the platform (Xamarin.Mac.dll)
							if (Platform == Platform.macOSModern) {
								mmp.Append ("--linksdkonly ");
								mmp.Append ($"--xml={Utils.Quote (Path.Combine (OutputDirectory, "bindings.xml"))} ");
							}
							else {
								// mmp default is to link everything
								mmp.Append ("--nolink ");
							}
							mmp.Append ("--registrar:static ");
							mmp.Append ($"--cache {Utils.Quote (cachedir)} ");
							if (Debug)
								mmp.Append ("--debug ");
							mmp.Append ("-p "); // generate a plist
							mmp.Append ($"--target-framework {GetTargetFramework ()} ");
							string extensionFlag = Extension ? "-fapplication-extension" : "";
							string forceLoad = $"-force_load \\\"{Path.GetFullPath (sdk_output_file)}\\\"";
							mmp.Append ($"--link_flags=\"{extensionFlag + " " + forceLoad}\"");
							if (!Utils.RunProcess ("/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/bin/mmp", mmp.ToString (), out exitCode))
								return exitCode;

							// Add the headers to the framework
							File.Copy (Path.Combine (OutputDirectory, "embeddinator.h"), Path.Combine (headers, "embeddinator.h"), true);
							File.Copy (Path.Combine (OutputDirectory, "bindings.h"), Path.Combine (headers, $"bindings.h"), true);
							// Create an umbrella header for the everything in the framework.
							File.WriteAllText (Path.Combine (headers, LibraryName + ".h"),
	@"
#include ""bindings.h""
#if defined(__i386__)
#include ""registrar-i386.h""
#elif defined(__x86_64__)
#include ""registrar-x86_64.h""
#else
#error Unknown architecture
#endif
");
							if (build_info.Architectures.Contains ("i386"))
								Utils.FileCopyIfExists (Path.Combine (cachedir, "registrar.h"), Path.Combine (headers, "registrar-i386.h"));
							if (build_info.Architectures.Contains ("x86_64"))
								Utils.FileCopyIfExists (Path.Combine (cachedir, "registrar.h"), Path.Combine (headers, "registrar-x86_64.h"));
							break;
						case Platform.iOS:
						case Platform.tvOS:
						case Platform.watchOS:
							var mtouch = new StringBuilder ();
							mtouch.Append (build_info.IsSimulator ? "--sim " : "--dev ");
							mtouch.Append ($"{Utils.Quote (appdir)} ");
							mtouch.Append ($"--abi={string.Join (",", build_info.Architectures)} ");
							mtouch.Append ($"--sdkroot {XcodeApp} ");
							mtouch.Append ($"--targetver {build_info.MinVersion} ");
							mtouch.Append ("--dsym:false ");
							mtouch.Append ("--msym:false ");
							mtouch.Append ($"--embeddinator ");
							foreach (var asm in Assemblies)
								mtouch.Append (Utils.Quote (Path.GetFullPath (asm.Location))).Append (" ");
							mtouch.Append ($"-r:{GetPlatformAssembly ()} ");
							mtouch.Append ($"--sdk {GetSdkVersion (build_info.Sdk.ToLower ())} ");
							mtouch.Append ("--linksdkonly ");
							mtouch.Append ($"--xml={Utils.Quote (Path.Combine (OutputDirectory, "bindings.xml"))} ");
							mtouch.Append ("--registrar:static ");
							mtouch.Append ($"--cache {Utils.Quote (cachedir)} ");
							if (Debug)
								mtouch.Append ("--debug ");
							mtouch.Append ($"--assembly-build-target=@all=framework={LibraryName}.framework ");
							mtouch.Append ($"--target-framework {GetTargetFramework ()} ");
							mtouch.Append ($"\"--gcc_flags=-force_load {Path.GetFullPath (sdk_output_file)}\" ");
							if (!Utils.RunProcess ("/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/bin/mtouch", mtouch.ToString (), out exitCode))
								return exitCode;

							// Add the headers to the framework
							File.Copy (Path.Combine (OutputDirectory, "embeddinator.h"), Path.Combine (headers, "embeddinator.h"), true);
							File.Copy (Path.Combine (OutputDirectory, "bindings.h"), Path.Combine (headers, $"bindings.h"), true);
							// Create an umbrella header for the everything in the framework.
							File.WriteAllText (Path.Combine (headers, LibraryName + ".h"),
	@"
#include ""bindings.h""
#if defined(__i386__)
#include ""registrar-i386.h""
#elif defined(__x86_64__)
#include ""registrar-x86_64.h""
#elif defined(__arm__)
#include ""registrar-arm32.h"" // this includes all 32-bit arm architectures.
#elif defined(__aarch64__)
#include ""registrar-arm64.h""
#else
#error Unknown architecture
#endif
");
							switch (Platform) {
								case Platform.iOS:
									if (build_info.IsSimulator) {
										Utils.FileCopyIfExists (Path.Combine (cachedir, "32", "registrar.h"), Path.Combine (headers, "registrar-i386.h"));
										Utils.FileCopyIfExists (Path.Combine (cachedir, "64", "registrar.h"), Path.Combine (headers, "registrar-x86_64.h"));
									}
									else {
										Utils.FileCopyIfExists (Path.Combine (cachedir, "32", "registrar.h"), Path.Combine (headers, "registrar-arm32.h"));
										Utils.FileCopyIfExists (Path.Combine (cachedir, "64", "registrar.h"), Path.Combine (headers, "registrar-arm64.h"));
									}
									break;
								case Platform.tvOS:
									Utils.FileCopyIfExists (Path.Combine (cachedir, "registrar.h"), Path.Combine (headers, build_info.IsSimulator ? "registrar-x86_64.h" : "registrar-arm64.h"));
									break;
								default:
									throw ErrorHelper.CreateError (99, "Internal error: invalid platform {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).", Platform);
							}
							break;
						default:
							throw ErrorHelper.CreateError (99, "Internal error: invalid platform {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).", Platform);
					}

					// Move the framework to the output directory
					var fwpath = Path.Combine (OutputDirectory, build_info.Sdk, $"{LibraryName}.framework");
					if (Directory.Exists (fwpath))
						Directory.Delete (fwpath, true);
					Directory.Move (fwdir, fwpath);
					Directory.Delete (appdir, true); // We don't need this anymore.
				}
			}

			var output_path = Path.Combine (OutputDirectory, output_file);
			if (!Utils.Lipo (lipo_files, output_path, out exitCode))
				return exitCode;

			if (!DSymUtil (output_path, out exitCode))
				return exitCode;

			if (CompilationTarget == CompilationTarget.Framework) {
				var fwpath = Path.Combine (OutputDirectory, $"{LibraryName}.framework");
				if (build_infos.Length == 2) {
					var dev_fwpath = Path.Combine (OutputDirectory, build_infos[0].Sdk, $"{LibraryName}.framework");
					var sim_fwpath = Path.Combine (OutputDirectory, build_infos[1].Sdk, $"{LibraryName}.framework");
					if (!MergeFrameworks (fwpath, dev_fwpath, sim_fwpath, out exitCode))
						return exitCode;
				}
				else {
					if (Directory.Exists (fwpath))
						Directory.Delete (fwpath, true);
					var fwsdkpath = Path.Combine (OutputDirectory, build_infos[0].Sdk, $"{LibraryName}.framework");
					Directory.Move (fwsdkpath, fwpath);
				}
				Console.WriteLine ($"Successfully created framework: {fwpath}");
			}
			else {
				Console.WriteLine ($"Successfully created library: {output_path}");
			}

			return 0;
		}

		// All files from both frameworks will be included.
		// For files present in both frameworks:
		// * The executables will be lipoed
		// * Info.plist will be manually merged.
		// * Headers: should be identical, so we just choose one of them.
		// * other files: show an error.
		static bool MergeFrameworks (string output, string deviceFramework, string simulatorFramework, out int exitCode)
		{
			if (deviceFramework[deviceFramework.Length - 1] == Path.DirectorySeparatorChar)
				deviceFramework = deviceFramework.Substring (0, deviceFramework.Length - 1);
			if (simulatorFramework[simulatorFramework.Length - 1] == Path.DirectorySeparatorChar)
				simulatorFramework = simulatorFramework.Substring (0, simulatorFramework.Length - 1);

			var name = Path.GetFileNameWithoutExtension (deviceFramework);
			var deviceFiles = Directory.GetFiles (deviceFramework, "*", SearchOption.AllDirectories);
			var simulatorFiles = Directory.GetFiles (simulatorFramework, "*", SearchOption.AllDirectories);

			Directory.CreateDirectory (output);
			var executables = new List<string> ();
			executables.Add (Path.Combine (deviceFramework, name));
			executables.Add (Path.Combine (simulatorFramework, name));
			if (!Utils.Lipo (executables, Path.Combine (output, name), out exitCode))
				return false;

			var relativeDeviceFiles = deviceFiles.Select ((v) => v.Substring (deviceFramework.Length + 1));
			var relativeSimulatorFiles = simulatorFiles.Select ((v) => v.Substring (simulatorFramework.Length + 1));
			var allFiles = relativeDeviceFiles.Concat (relativeSimulatorFiles).ToList ();
			allFiles.RemoveAll ((v) => v == name); // the executable, which we've already handled (lipoed).

			var groupedFiles = allFiles.GroupBy ((v) => v);
			foreach (var @group in groupedFiles) {
				var file = @group.Key;
				var unique = @group.Count () == 1;
				var targetPath = Path.Combine (output, file);
				Directory.CreateDirectory (Path.GetDirectoryName (targetPath));
				if (unique) {
					// A single file, just copy it.
					string srcPath;
					if (relativeDeviceFiles.Contains (file)) {
						srcPath = Path.Combine (deviceFramework, file);
					}
					else {
						srcPath = Path.Combine (simulatorFramework, file);
					}
					File.Copy (srcPath, targetPath, true);
				}
				else {
					// Same file in both device and simulator frameworks.
					if (file == "Info.plist") {
						MergeInfoPlists (Path.Combine (output, file), Path.Combine (deviceFramework, file), Path.Combine (simulatorFramework, file));
					}
					else if (file.StartsWith ("Headers/", StringComparison.Ordinal)) {
						// Headers are identical between simulator and device, so no special processing needed, just choose one of them.
						File.Copy (Path.Combine (deviceFramework, file), targetPath, true);
					}
					else {
						throw ErrorHelper.CreateError (10, $"Can't merge the frameworks '{simulatorFramework}' and '{deviceFramework}' because the file '{file}' exists in both.");
					}
				}
			}

			return true;
		}

		// Merge CFBundleSupportPlatforms from both Info.plists.
		// At the moment all other values are only from the first plist.
		static void MergeInfoPlists (string output, string a, string b)
		{
			var adoc = new XmlDocument ();
			var bdoc = new XmlDocument ();
			XmlReaderSettings settings = new XmlReaderSettings () {
				XmlResolver = null,
				DtdProcessing = DtdProcessing.Parse
			};

			using (var srA = new StreamReader (a, Encoding.UTF8, true))
 			using (var readerA = XmlReader.Create (srA, settings))
 			using (var srB = new StreamReader (b, Encoding.UTF8, true))
 			using (var readerB = XmlReader.Create (srB, settings)) {
				adoc.Load (readerA);
				bdoc.Load (readerB);

				var a_supported_platforms = ((XmlNode)adoc.SelectSingleNode ("/plist/dict/key[text()='CFBundleSupportedPlatforms']/following-sibling::array"));
				var b_supported_platforms = ((XmlNode)bdoc.SelectSingleNode ("/plist/dict/key[text()='CFBundleSupportedPlatforms']/following-sibling::array"));

				foreach (XmlNode b_platform in b_supported_platforms.ChildNodes) {
					var node = adoc.ImportNode (b_platform, true);
					a_supported_platforms.AppendChild (node);
				}

				var writerSettings = new XmlWriterSettings ();
				writerSettings.Encoding = new UTF8Encoding (false);
				writerSettings.IndentChars = "    ";
				writerSettings.Indent = true;
				using (var writer = XmlWriter.Create (output, writerSettings))
					adoc.Save (writer);

				// Apple's plist reader does not like empty internal subset declaration,
				// even though this is allowed according to the xml spec: http://stackoverflow.com/a/6192048/183422
				// So manually fix the xml :(
				var contents = File.ReadAllText (output);
				contents = contents.Replace (
					@"<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd""[]>",
					@"<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">");
				File.WriteAllText (output, contents);
			}
		}

		string GetSdkVersion (string sdk)
		{
			int exitCode;
			string output;
			if (!Utils.RunProcess ("xcrun", $"--show-sdk-version --sdk {sdk}", out exitCode, out output))
				throw ErrorHelper.CreateError (7, $"Could not get the sdk version for '{sdk}'");
			return output.Trim ();
		}

		string GetPlatformAssembly ()
		{
			switch (Platform) {
				case Platform.macOS:
					throw new NotImplementedException ("platform assembly for macOS"); // We need to know full/mobile
				case Platform.macOSSystem:
				case Platform.macOSFull:
					return "/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/lib/mono/4.5/Xamarin.Mac.dll";
				case Platform.macOSModern:
					return "/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/lib/mono/Xamarin.Mac/Xamarin.Mac.dll";
				case Platform.iOS:
					return "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/Xamarin.iOS/Xamarin.iOS.dll";
				case Platform.tvOS:
					return "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/Xamarin.TVOS/Xamarin.TVOS.dll";
				case Platform.watchOS:
					return "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/Xamarin.WatchOS/Xamarin.WatchOS.dll";
				default:
					throw ErrorHelper.CreateError (99, "Internal error: invalid platform {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).", Platform);
			}
		}

		string GetTargetFramework ()
		{
			switch (Platform) {
				case Platform.macOSSystem:
					return "Xamarin.Mac,Version=v4.5,Profile=System";
				case Platform.macOSFull:
					return "Xamarin.Mac,Version=v4.5,Profile=Full";
				case Platform.macOSModern:
					return "Xamarin.Mac,Version=v2.0,Profile=Mobile";
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

		bool DSymUtil (string input, out int exitCode)
		{
			exitCode = 0;

			if (!Debug)
				return true;

			string output;
			switch (CompilationTarget) {
				case CompilationTarget.StaticLibrary:
				case CompilationTarget.Framework:
					return true;
				case CompilationTarget.SharedLibrary:
					output = input + ".dSYM";
					break;
				default:
					throw ErrorHelper.CreateError (99, "Internal error: invalid compilation target {0}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).", CompilationTarget);
			}

			var dsymutil_options = new StringBuilder ("dsymutil ");
			dsymutil_options.Append (Utils.Quote (input)).Append (" ");
			dsymutil_options.Append ($"-o {Utils.Quote (output)} ");
			return Utils.Xcrun (dsymutil_options, out exitCode);
		}
	}
}
