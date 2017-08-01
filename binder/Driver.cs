﻿﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;
using MonoEmbeddinator4000.Generators;
using MonoEmbeddinator4000.Passes;
using BindingContext = CppSharp.Generators.BindingContext;

namespace MonoEmbeddinator4000
{
    public partial class Driver
    {
        public Project Project { get; private set; }
        public Options Options { get; private set; }

        public BindingContext Context { get; private set; }
        public Generator Generator { get; private set; }

        public List<IKVM.Reflection.Assembly> Assemblies { get; private set; }

        public ProjectOutput Output { get; private set; }

        public Driver(Project project, Options options)
        {
            Project = project;
            Options = options;

            Options.GenerateSupportFiles = 
                Options.GeneratorKind == GeneratorKind.C ||
                Options.GeneratorKind == GeneratorKind.CPlusPlus ||
                Options.GeneratorKind == GeneratorKind.ObjectiveC;

            Assemblies = new List<IKVM.Reflection.Assembly>();

            Context = new BindingContext(options);
            Context.ASTContext = new ASTContext();

            if (Options.Verbose)
                Diagnostics.Level = DiagnosticKind.Debug;

            Declaration.QualifiedNameSeparator = "_";
        }

        bool Parse()
        {
            var parser = new Parser();

            foreach (var assembly in Project.Assemblies)
            {
                parser.AddAssemblyResolveDirectory(Path.GetDirectoryName(assembly));
            }

            if (Options.Compilation.Platform == TargetPlatform.Android)
            {
                foreach (var dir in XamarinAndroid.TargetFrameworkDirectories)
                {
                    parser.AddAssemblyResolveDirectory(dir);
                }
            }

            parser.OnAssemblyParsed += HandleAssemblyParsed;

            return parser.Parse(Project);
        }

        void Process()
        {
            Generator = CreateGenerator();

            var astGenerator = new ASTGenerator(Context.ASTContext, Options);

            foreach (var assembly in Assemblies)
                astGenerator.Visit(assembly);

            Context.TranslationUnitPasses.Passes.AddRange(new List<TranslationUnitPass>
            {
                new CheckReservedKeywords(),
                new GenerateObjectTypesPass(),
                new GenerateArrayTypes(),
                new CheckIgnoredDeclsPass { CheckDecayedTypes = false },
                new RenameDuplicatedDeclsPass(),
                new CheckDuplicatedNamesPass(),
                new FieldToGetterSetterPropertyPass()
            });

            Generator.SetupPasses();

            Context.TranslationUnitPasses.Passes.AddRange(new TranslationUnitPass[]
            {
                new CheckReservedKeywords(),
            });

            Context.RunPasses();
        }

        void GenerateSupportFiles()
        {
            // Search for the location of support directory and bundle files with output.
            try
            {
                var path = Helpers.FindDirectory("support");

                foreach (var file in Directory.EnumerateFiles(path))
                {
                    // Skip Objective-C support files if we are not targetting it.
                    if (Options.GeneratorKind != GeneratorKind.ObjectiveC &&
                        (Path.GetExtension(file) == ".m" || file.Contains("objc")))
                        continue;

                    Output.WriteOutput(Path.GetFileName(file), File.ReadAllText(file));
                }
            }
            catch (Exception)
            {
                Diagnostics.Warning("Could not find directory with support API.");
            }
        }

        void Generate()
        {
            Output = new ProjectOutput();

            foreach (var unit in Context.ASTContext.TranslationUnits)
            {
                var outputs = Generator.Generate(new[] { unit });

                foreach (var output in outputs)
                {
                    output.Process();
                    var text = output.Generate();

                    Output.WriteOutput(output.FilePath, text);
                }
            }

            if (Options.GenerateSupportFiles)
                GenerateSupportFiles();
        }

        Generator CreateGenerator()
        {
            Generator generator = null;
            switch (Options.GeneratorKind)
            {
                case GeneratorKind.C:
                    generator = new CGenerator(Context);
                    break;
                case GeneratorKind.ObjectiveC:
                    generator = new ObjCGenerator(Context);
                    break;
                case GeneratorKind.Java:
                    generator = new JavaGenerator(Context);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return generator;
        }

        bool WriteFiles()
        {
            if (!Directory.Exists(Options.OutputDir))
                Directory.CreateDirectory(Options.OutputDir);

            foreach (var output in Output.Files)
            {
                var path = output.Key;

                var outputPath = Path.Combine(Options.OutputDir,
                    Path.GetDirectoryName(path));

                // Make sure the target directory exists.
                Directory.CreateDirectory(outputPath);

                var fullPath = Path.Combine(outputPath, Path.GetFileName(path));

                var outputStream = output.Value;
                outputStream.Position = 0;

                using (var outputFile = File.Create(fullPath))
                    outputStream.CopyTo(outputFile);

                Diagnostics.Message("Generated: {0}", path);
            }

            if (Options.GeneratorKind == GeneratorKind.Java && Options.Compilation.Platform == TargetPlatform.Android)
            {
                Diagnostics.Message("Generating Java stubs...");
                var project = XamarinAndroidBuild.GenerateJavaStubsProject(Assemblies, Options.OutputDir);
                if (!MSBuild(project))
                    return false;
            }

            return true;
        }

        bool ValidateAssemblies()
        {
            foreach (var assembly in Project.Assemblies)
            {
                var file = Path.GetFullPath(assembly);

                if (File.Exists(file))
                    continue;

                Diagnostics.Error("Could not find assembly '{0}'", assembly);
                return false;
            }

            return true;
        }

        public bool Run()
        {
            if (!ValidateAssemblies())
                return false;

            Project.BuildInputs();

            Diagnostics.Message("Parsing assemblies...");
            Diagnostics.PushIndent();
            if (!Parse())
                return false;
            Diagnostics.PopIndent();

            Diagnostics.Message("Processing assemblies...");
            Diagnostics.PushIndent();
            Process();
            Diagnostics.PopIndent();

            Diagnostics.Message("Generating binding code...");
            Diagnostics.PushIndent();
            Generate();
            if (!WriteFiles())
                return false;
            Diagnostics.PopIndent();

            if (Options.CompileCode)
            {
                Diagnostics.Message("Compiling binding code...");
                Diagnostics.PushIndent();
                var compiled = CompileCode();
                Diagnostics.PopIndent();

                if (!compiled)
                {
                    Diagnostics.Message("Failed to compile generated code.");
                    return false;
                }
            }

            return true;
        }

        void HandleParserResult<T>(ParserResult<T> result)
        {
            var file = result.Input.FullPath;
            if (file.StartsWith(result.Input.BasePath, StringComparison.Ordinal))
            {
                file = file.Substring(result.Input.BasePath.Length);
                file = file.TrimStart('\\');
                file = file.TrimStart('/');
            }

            switch (result.Kind)
            {
                case ParserResultKind.Success:
                    Diagnostics.Message("Parsed '{0}'", file);
                    break;
                case ParserResultKind.Error:
                    Diagnostics.Message("Error parsing '{0}'", file);
                    break;
                case ParserResultKind.FileNotFound:
                    Diagnostics.Message("File '{0}' was not found", file);
                    break;
            }

            foreach (var diag in result.Diagnostics)
            {
                Diagnostics.Message("{0}({1},{2}): {3}: {4}",
                    diag.FileName, diag.LineNumber, diag.ColumnNumber,
                    diag.Level.ToString().ToLower(), diag.Message);
            }
        }

        bool HandleAssemblyParsed(ParserResult<IKVM.Reflection.Assembly> result)
        {
            HandleParserResult(result);

            if (result.Output.GetReferencedAssemblies().Any(ass => ass.Name == "Mono.Android") &&
                Options.Compilation.Platform != TargetPlatform.Android)
            {
                Console.Error.WriteLine("Assembly references Mono.Android.dll, plase specify target platform as Android.");

                result.Kind = ParserResultKind.Error;
                return false;
            }

            if (result.Kind != ParserResultKind.Success)
                return false;

            //NOTE: this can happen if multiple generators are running
            if (!Assemblies.Contains(result.Output))
                Assemblies.Add(result.Output);

            return true;
        }
    }
}
