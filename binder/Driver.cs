using System;
using System.Collections.Generic;
using System.IO;
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
            parser.OnAssemblyParsed += HandleAssemblyParsed;

            return parser.Parse(Project);
        }

        void Process()
        {
            Generator = CreateGenerator();

            var astGenerator = new ASTGenerator(Context.ASTContext, Options);

            foreach (var assembly in Assemblies)
                astGenerator.Visit(assembly);

            var passes = new List<TranslationUnitPass>
            {
                new CheckReservedKeywords(),
                new GenerateObjectTypesPass(),
                new GenerateArrayTypes(),
                new CheckIgnoredDeclsPass { CheckDecayedTypes = false }
            };

            if (Options.GeneratorKind == GeneratorKind.C ||
                Options.GeneratorKind == GeneratorKind.ObjectiveC)
            {
                passes.Add(new RenameEnumItemsPass());
                passes.Add(new FixMethodParametersPass());
            }

            Generator.SetupPasses();

            passes.AddRange(new TranslationUnitPass[]
            {
                new RenameDuplicatedDeclsPass(),
                new CheckDuplicatedNamesPass()
            });

            Context.TranslationUnitPasses.Passes.AddRange(passes);

            Context.RunPasses();
        }

        public static string GetSupportDirectory()
        {
            var directory = new DirectoryInfo (Directory.GetCurrentDirectory());

            while (directory != null)
            {
                var path = Path.Combine(directory.FullName, "support");

                if (Directory.Exists(path))
                    return path;

                directory = directory.Parent;
            }

            throw new Exception("Support directory was not found");
        }

        void GenerateSupportFiles()
        {
            // Search for the location of support directory and bundle files with output.
            try
            {
                var path = GetSupportDirectory();

                foreach (var file in Directory.EnumerateFiles(path))
                    Output.WriteOutput(Path.GetFileName(file), File.ReadAllText(file));
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
                    Diagnostics.Error($"The Generator {Options.GeneratorKind} is not supported yet. Please choose a different one.");
                    Environment.Exit(-1);
                    break;
            }

            return generator;
        }

        void WriteFiles()
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

        public void Run()
        {
            if (!ValidateAssemblies())
                return;

            Project.BuildInputs();

            Diagnostics.Message("Parsing assemblies...");
            Diagnostics.PushIndent();
            if (!Parse())
                return;
            Diagnostics.PopIndent();

            Diagnostics.Message("Processing assemblies...");
            Diagnostics.PushIndent();
            Process();
            Diagnostics.PopIndent();

            Diagnostics.Message("Generating binding code...");
            Diagnostics.PushIndent();
            Generate();
            WriteFiles();
            Diagnostics.PopIndent();

            if (Options.CompileCode)
            {
                Diagnostics.Message("Compiling binding code...");
                Diagnostics.PushIndent();
                CompileCode();
                Diagnostics.PopIndent();
            }
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

        void HandleAssemblyParsed(ParserResult<IKVM.Reflection.Assembly> result)
        {
            HandleParserResult(result);

            if (result.Kind != ParserResultKind.Success)
                return;

            Assemblies.Add(result.Output);
        }
    }
}
