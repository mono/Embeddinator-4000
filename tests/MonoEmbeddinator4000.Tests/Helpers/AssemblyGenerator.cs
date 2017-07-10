using System;
using System.CodeDom.Compiler;
using System.IO;
using Microsoft.CSharp;

namespace MonoEmbeddinator4000.Tests
{
    public static class AssemblyGenerator
    {
        public static void CreateFromResource(string resourceFile, CompilerParameters parameters)
        {
            string sourceCode;
            using (var stream = typeof(AssemblyGenerator).Assembly.GetManifestResourceStream($"MonoEmbeddinator4000.Tests.ResourceDesigner.{resourceFile}.cs"))
            using (var reader = new StreamReader(stream))
            {
                sourceCode = reader.ReadToEnd();
            }

            CreateFromSource(sourceCode, parameters);
        }

        public static void CreateFromSource(string sourceCode, CompilerParameters parameters)
        {
            var csc = new CSharpCodeProvider();
            var results = csc.CompileAssemblyFromSource(parameters, sourceCode);
            if (results.Errors.HasErrors)
            {
                throw new Exception(results.Errors[0].ToString());
            }
        }
    }
}
