using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MonoEmbeddinator4000
{
    public enum ParserDiagnosticLevel
    {
        Ignored,
        Note,
        Warning,
        Error,
        Fatal
    }

    public struct ParserDiagnostic
    {
        public string FileName;
        public string Message;
        public ParserDiagnosticLevel Level;
        public int LineNumber;
        public int ColumnNumber;
    }

    public enum ParserResultKind
    {
        Success,
        Error,
        FileNotFound
    }

    public class ParserResult<T>
    {
        public ParserResult()
        {
            Kind = ParserResultKind.Success;
            Diagnostics = new List<ParserDiagnostic>();
        }

        public ProjectInput Input;
        public ParserResultKind Kind;
        public List<ParserDiagnostic> Diagnostics;
        public string Message;

        public T Output;

        public bool HasErrors
        {
            get
            {
                return Diagnostics.Any(diagnostic =>
                    diagnostic.Level == ParserDiagnosticLevel.Error ||
                    diagnostic.Level == ParserDiagnosticLevel.Fatal);
            }
        }
    }

    public delegate void ParserHandler<T>(ProjectInput input, ParserResult<T> result);

    public class Parser
    {
        public delegate void ParsedDelegate<T>(ParserResult<T> result);

        public ParsedDelegate<IKVM.Reflection.Assembly> OnAssemblyParsed = delegate { };

        public bool AllowMissingAssembly;

        List<string> AssemblyResolveDirs;

        IKVM.Reflection.Universe Universe;

        public Parser()
        {
            AssemblyResolveDirs = new List<string>();
            Universe = new IKVM.Reflection.Universe(IKVM.Reflection.UniverseOptions.MetadataOnly);
            Universe.AssemblyResolve += AssemblyResolve;
        }

        public void AddAssemblyResolveDirectory(string dir)
        {
            AssemblyResolveDirs.Add(dir);
        }

        IKVM.Reflection.Assembly AssemblyResolve (object sender, IKVM.Reflection.ResolveEventArgs args)
        {
            var universe = ((IKVM.Reflection.Universe)sender);

            var assembly = universe.DefaultResolver(args.Name, throwOnError: false);
            if (assembly != null)
                return assembly;

            var assemblyName = new IKVM.Reflection.AssemblyName(args.Name);
            foreach (var dir in AssemblyResolveDirs)
            {
                var assemblyPath = Path.Combine(dir, $"{assemblyName.Name}.dll");

                if (!File.Exists(assemblyPath))
                    continue;

                return universe.LoadFile(assemblyPath);
            }

            return AllowMissingAssembly ? universe.CreateMissingAssembly(args.Name) : null;
        }

        public void ParseAssembly(ProjectInput input, ParserResult<IKVM.Reflection.Assembly> result)
        {
            try
            {
                try
                {
                    var res = Universe.LoadFile(input.FullPath);
                    result.Output = res;
                }
                catch (Exception ex)
                {
                    result.Message = ex.ToString();
                    result.Kind = ParserResultKind.Error;
                }
            }
            finally
            {
                OnAssemblyParsed(result);
            }
        }

        ParserResult<T> ParseInput<T>(ProjectInput input, ParserHandler<T> handler)
        {
            var result = new ParserResult<T>
            {
                Input = input
            };

            if (!File.Exists(input.FullPath))
            {
                result.Kind = ParserResultKind.FileNotFound;
                return result;
            }

            handler(input, result);
            return result;
        }

        public bool Parse(Project project)
        {
            var hasErrors = false;

            foreach (var input in project.AssemblyInputs)
            {
                var result = ParseInput<IKVM.Reflection.Assembly>(input, ParseAssembly);
                hasErrors |= result.HasErrors;
            }

            return !hasErrors;
        }
    }
}
