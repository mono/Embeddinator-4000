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

        IKVM.Reflection.Universe Universe;

        public Parser()
        {
            Universe = new IKVM.Reflection.Universe(IKVM.Reflection.UniverseOptions.MetadataOnly);
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
