using System.Collections.Generic;
using System.IO;
using MonoManagedToNative.Generators;
using System;

namespace MonoManagedToNative
{
    public class Driver
    {
        public Options Options { get; private set; }
        public IDiagnostics Diagnostics { get; private set; }

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
        }

        bool Parse()
        {
            var parser = new Parser(Options, Diagnostics);
            parser.OnAssemblyParsed += HandleAssemblyParsed;

            return parser.Parse(Options.Project);
        }

        void Process()
        {

        }

        void Generate()
        {
            Output = new ProjectOutput();

            Generator generator = null;
            switch (Options.Language)
            {
                case GeneratorKind.C:
                    generator = new CGenerator(this);
                    break;
                default:
                    throw new NotImplementedException();
            }

            foreach (var assembly in Assemblies)
            {
                var templates = generator.Generate(assembly);

                foreach (var template in templates)
                {
                    template.Process();
                    var text = template.Generate();
                    var path = string.Format("{0}.{1}", template.Name, template.FileExtension);

                    Output.WriteOutput(path, text);
                }
            }
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

        public void Run()
        {
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
        }

        void HandleParserResult<T>(ParserResult<T> result)
        {
            var file = result.Input.FullPath;
            if (file.StartsWith(result.Input.BasePath))
            {
                file = file.Substring(result.Input.BasePath.Length);
                file = file.TrimStart('\\');
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
