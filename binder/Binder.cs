using System;
using System.Collections.Generic;
using System.IO;
using MonoManagedToNative.Generators;

namespace MonoManagedToNative
{
    public class Binder
    {
        static string Generator;
        static string MonoIncDir;
        static string OutputDir;
        static List<string> Assemblies;

        static void ParseCommandLineArgs(string[] args)
        {
            var showHelp = args.Length == 0;

            var optionSet = new Mono.Options.OptionSet() {
                { "gen=", "target generator (C, C++)", v => Generator = v },
                { "o|out=", "output directory", v => OutputDir = v },
                { "mono=", "Mono include directory", v => MonoIncDir = v },
                { "h|help",  "show this message and exit",  v => showHelp = v != null },
            };

            Generator = "C";

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
            }

            throw new NotSupportedException("Unknown target generator: " + gen);
        }

        static void Main(string[] args)
        {
            ParseCommandLineArgs(args);

            var options = new Options();
            options.OutputDir = OutputDir;

            var generator = ConvertToGeneratorKind(Generator);
            options.Language = generator;

            var currentDir = Directory.GetCurrentDirectory();
            options.Project.AssemblyDirs.Add(currentDir);

            foreach (var assembly in Assemblies)
                options.Project.Assemblies.Add(assembly);

            var driver = new Driver(options);
            driver.Run();
        }
    }
}
