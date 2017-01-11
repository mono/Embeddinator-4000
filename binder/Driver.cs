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
        public Options Options { get; private set; }
        public IDiagnostics Diagnostics { get; private set; }

        public BindingContext Context { get; private set; }

        public List<IKVM.Reflection.Assembly> Assemblies { get; private set; }

        public ProjectOutput Output { get; private set; }

        public Driver(Options options, IDiagnostics diagnostics = null)
        {
            Options = options;
            Diagnostics = diagnostics;

            if (Diagnostics == null)
                Diagnostics = new TextDiagnosticPrinter();

            if (Options.OutputDir == null)
                Options.OutputDir = Directory.GetCurrentDirectory();

            Assemblies = new List<IKVM.Reflection.Assembly>();

            var driverOptions = new DriverOptions { GeneratorKind = options.Language };
            Context = new BindingContext(Diagnostics, driverOptions);
            Context.ASTContext = new ASTContext();

            Declaration.QualifiedNameSeparator = "_";

            SetupTypePrinter();
        }

        void SetupTypePrinter()
        {
            CppSharp.AST.Type.TypePrinterDelegate = type =>
            {
                CppTypePrintFlavorKind kind = CppTypePrintFlavorKind.Cpp;
                switch (Options.Language)
                {
                case GeneratorKind.C:
                    kind = CppTypePrintFlavorKind.C;
                    break;
                case GeneratorKind.ObjectiveC:
                    kind = CppTypePrintFlavorKind.ObjC;
                    break;
                }

                var typePrinter = new CppTypePrinter { PrintFlavorKind = kind };
                return type.Visit(typePrinter);
            };
        }

        bool Parse()
        {
            var parser = new Parser();
            parser.OnAssemblyParsed += HandleAssemblyParsed;

            return parser.Parse(Options.Project);
        }

        void Process()
        {
            var astGenerator = new ASTGenerator(Context.ASTContext, Options);

            foreach (var assembly in Assemblies)
                astGenerator.Visit(assembly);

            Context.TranslationUnitPasses.AddPass(new CheckKeywordsPass());
            Context.TranslationUnitPasses.AddPass(new FixMethodParametersPass());
            Context.TranslationUnitPasses.AddPass(new GenerateObjectTypesPass());
            Context.TranslationUnitPasses.AddPass(new GenerateArrayTypes());
            Context.TranslationUnitPasses.AddPass(new CheckIgnoredDeclsPass());

            if (Options.Language != GeneratorKind.CPlusPlus)
                Context.TranslationUnitPasses.AddPass(new RenameEnumItemsPass());

            Context.TranslationUnitPasses.AddPass(new RenameDuplicatedDeclsPass());
            Context.TranslationUnitPasses.AddPass(new CheckDuplicatedNamesPass());
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

        void GenerateSupportFiles(ProjectOutput output)
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

            Generators.Generator generator = null;
            switch (Options.Language)
            {
                case GeneratorKind.C:
                    generator = new CGenerator(Context, Options);
                    break;
                case GeneratorKind.ObjectiveC:
                    generator = new ObjCGenerator(Context, Options);
                    break;
                default:
                    throw new NotImplementedException();
            }

            foreach (var unit in Context.ASTContext.TranslationUnits)
            {
                var templates = generator.Generate(unit);

                foreach (var template in templates)
                {
                    template.Process();
                    var text = template.Generate();
                    var path = string.Format("{0}.{1}", template.Name, template.FileExtension);

                    Output.WriteOutput(path, text);
                }
            }

            if (Options.GenerateSupportFiles)
                GenerateSupportFiles(Output);
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
            foreach (var assembly in Options.Project.Assemblies)
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

            Options.Project.BuildInputs();

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
                Diagnostics.Message(string.Format("{0}({1},{2}): {3}: {4}",
                    diag.FileName, diag.LineNumber, diag.ColumnNumber,
                    diag.Level.ToString().ToLower(), diag.Message));
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
