using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppSharp;
using CppSharp.Generators;

namespace MonoEmbeddinator4000
{
    public class CLI
    {
        static List<GeneratorKind> Generators;
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
                .Select(s => s.StartsWith("VS", StringComparison.InvariantCulture) ? s.Substring(2) : s));

            var optionSet = new Mono.Options.OptionSet() {
                { "gen=", "target generator (C, C++, Obj-C, Java)", v => Generators.Add(ConvertToGeneratorKind(v)) },
                { "p|platform=", "target platform (iOS, macOS, Android, Windows)", v => Platform = v },
                { "o|out|outdir=", "output directory", v => OutputDir = v },
                { "c|compile", "compiles the generated output", v => CompileCode = true },
                { "d|debug", "enables debug mode for generated native and managed code", v => DebugMode = true },
                { "t|target=", "compilation target (static, shared, app)", v => Target = ConvertToCompilationTarget(v) },
                { "dll|shared", "compiles as a shared library", v => Target = CompilationTarget.SharedLibrary },
                { "static", "compiles as a static library", v => Target = CompilationTarget.StaticLibrary },
                { "vs=", $"Visual Studio version for compilation: {vsVersions} (defaults to Latest)", v => VsVersion = v },
                { "v|verbose", "generates diagnostic verbose output", v => Verbose = true },
                { "h|help",  "show this message and exit",  v => showHelp = v != null },
            };

            Generators = new List<GeneratorKind>();
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

        static CompilationTarget ConvertToCompilationTarget(string target)
        {
            switch(target.ToLowerInvariant())
            {
            case "static":
                return CompilationTarget.StaticLibrary;
            case "shared":
                return CompilationTarget.SharedLibrary;
            case "app":
            case "exe":
                return CompilationTarget.Application;
            }

            throw new NotSupportedException("Unknown compilation target: " + target);
        }

        static GeneratorKind ConvertToGeneratorKind(string gen)
        {
            switch(gen.ToLowerInvariant())
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
            switch (platform.ToLowerInvariant())
            {
            case "windows":
                return TargetPlatform.Windows;
            case "android":
                return TargetPlatform.Android;
            case "osx":
            case "macosx":
            case "macos":
            case "mac":
                return TargetPlatform.MacOS;
            case "ios":
                return TargetPlatform.iOS;
            case "watchos":
                return TargetPlatform.WatchOS;
            case "tvos":
                return TargetPlatform.TVOS;
            }

            throw new NotSupportedException ("Unknown target platform: " + platform);
        }

        static bool SetupOptions(Options options)
        {
            options.Verbose = Verbose;
            options.OutputDir = OutputDir;
            options.CompileCode = CompileCode;
            options.Compilation.Target = Target;
            options.Compilation.DebugMode = DebugMode;

            if (options.OutputDir == null)
                options.OutputDir = Directory.GetCurrentDirectory();

            if (Generators.Count == 0)
            {
                Console.Error.WriteLine("Please specify a target generator.");
                return false;
            }

            if (string.IsNullOrEmpty(Platform))
            {
                Console.Error.WriteLine("Please specify a target platform.");
                return false;
            }

            //NOTE: Choosing Java generator, needs to imply the C generator
            if (Generators.Contains(GeneratorKind.Java) && !Generators.Contains(GeneratorKind.C))
            {
                Generators.Insert(0, GeneratorKind.C);
            }

            var targetPlatform = ConvertToTargetPlatform(Platform);
            options.Compilation.Platform = targetPlatform;

            var vsVersion = ConvertToVsVersion(VsVersion);
            options.Compilation.VsVersion = vsVersion;

            return true;
        }

        static void Main(string[] args)
        {
            ParseCommandLineArgs(args);

            var options = new Options();
            if (!SetupOptions(options))
                return;

            var project = new Project();

            var currentDir = Directory.GetCurrentDirectory();
            project.AssemblyDirs.Add(currentDir);

            foreach (var assembly in Assemblies)
                project.Assemblies.Add(assembly);

            foreach (var generator in Generators)
            {
                options.GeneratorKind = generator;

                var driver = new Driver(project, options);

                driver.Run();
            }
        }
    }
}