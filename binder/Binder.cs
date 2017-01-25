using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppSharp;
using CppSharp.Generators;

namespace MonoEmbeddinator4000
{
    public class Binder
    {
        static string Generator;
        static string Platform;
        static string OutputDir;
        static string VsVersion;
        static List<string> Assemblies;
        static bool CompileCode;
        static bool Verbose;
        static CompilationTarget Target;
        static bool DebugMode;

        static void ParseCommandLineArgs(string[] args)
        {
            var showHelp = args.Length == 0;

            string vsVersions = string.Join(", ", 
                Enum.GetNames(typeof(VisualStudioVersion))
                .Select(s => s.StartsWith("VS", StringComparison.InvariantCulture) ? s.Substring(2) : s)
                );
            var optionSet = new Mono.Options.OptionSet() {
                { "gen=", "target generator (C, C++, Obj-C)", v => Generator = v },
                { "p|platform=", "target platform (iOS, macOS, Android)", v => Platform = v },
                { "o|out|outdir=", "output directory", v => OutputDir = v },
                { "c|compile", "compiles the generated output", v => CompileCode = true },
                { "d|debug", "enables debug mode for generated native and managed code", v => DebugMode = true },
                { "dll|shared", "compiles as a static library", v => Target = CompilationTarget.SharedLibrary },
                { "static", "compiles as a static library", v => Target = CompilationTarget.StaticLibrary },
                { "vs=", $"Visual Studio version for compilation: {vsVersions} (defaults to Latest)", v => VsVersion = v },
                { "v|verbose", "generates diagnostic verbose output", v => Verbose = true },
                { "h|help",  "show this message and exit",  v => showHelp = v != null },
            };

            Generator = "C";
            VsVersion = "latest";

            try
            {
                Assemblies = optionSet.Parse(args);
            }
            catch (Mono.Options.OptionException e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(0);
            }

            if (showHelp)
            {
                // Print usage and exit.
                Console.WriteLine("{0} [options]+ ManagedAssembly.dll", AppDomain.CurrentDomain.FriendlyName);
                Console.WriteLine("Generates target language bindings for interop with managed code.");
                Console.WriteLine();
                optionSet.WriteOptionDescriptions(Console.Out);
                Environment.Exit(0);
            }

            if (Assemblies == null || Assemblies.Count == 0)
            {
                Console.WriteLine("At least one managed assembly must be passed as input.");
                Environment.Exit(0);
            }
        }

        static GeneratorKind ConvertToGeneratorKind(string gen)
        {
            switch(gen.ToLower())
            {
            case "c":
                return GeneratorKind.C;
            case "c++":
            case "cpp":
                return GeneratorKind.CPlusPlus;
            case "objc":
            case "obj-c":
            case "objectivec":
            case "objective-c":
                return GeneratorKind.ObjectiveC;
            case "java":
                return GeneratorKind.Java;
            }

            throw new NotSupportedException("Unknown target generator: " + gen);
        }

        static VisualStudioVersion ConvertToVsVersion(string version)
        {
            if (string.Equals(version, "latest", StringComparison.InvariantCultureIgnoreCase))
                return VisualStudioVersion.Latest;

            VisualStudioVersion result;
            if (Enum.TryParse("VS" + version, out result))
                return result;

            throw new NotSupportedException("Unknown Visual Studio version: " + version);
        }

        static TargetPlatform ConvertToTargetPlatform(string platform)
        {
            switch (platform.ToLower())
            {
            case "windows":
                return TargetPlatform.Windows;
            case "android":
                return TargetPlatform.Android;
            case "osx":
            case "macosx":
            case "macos":
                return TargetPlatform.MacOS;
            case "ios":
                return TargetPlatform.iOS;
            case "watchos":
                return TargetPlatform.WatchOS;
            case "tvos":
                return TargetPlatform.TVOS;
            }

            throw new NotSupportedException("Unknown target platform: " + platform);
        }

        static void Main(string[] args)
        {
            ParseCommandLineArgs(args);

            var options = new Options();
            options.OutputDir = OutputDir;
            options.CompileCode = CompileCode;
            options.Target = Target;
            options.DebugMode = DebugMode;

            var generator = ConvertToGeneratorKind(Generator);
            options.Language = generator;

            var targetPlatform = ConvertToTargetPlatform(Platform);
            options.Platform = targetPlatform;

            var vsVersion = ConvertToVsVersion(VsVersion);
            options.VsVersion = vsVersion;

            var currentDir = Directory.GetCurrentDirectory();
            options.Project.AssemblyDirs.Add(currentDir);

            foreach (var assembly in Assemblies)
                options.Project.Assemblies.Add(assembly);

            var driver = new Driver(options);

            if (Verbose)
                driver.Diagnostics.Level = DiagnosticKind.Debug;

            driver.Run();
        }
    }
}