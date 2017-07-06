﻿using System;
using System.CodeDom.Compiler;
using System.IO;
using IKVM.Reflection;
using Microsoft.CSharp;

namespace MonoEmbeddinator4000.Tests
{
    public static class Samples
    {
        public static Assembly LoadFile(Universe universe, string resourceFile)
        {
            var csc = new CSharpCodeProvider();
            var temp = Path.GetTempFileName();

            string source;
            using (var stream = typeof(Samples).Assembly.GetManifestResourceStream($"MonoEmbeddinator4000.Tests.ResourceDesigner.{resourceFile}.cs"))
            using (var reader = new StreamReader(stream))
            {
                source = reader.ReadToEnd();
            }

            var parameters = new CompilerParameters
            {
                OutputAssembly = temp,
            };
            parameters.ReferencedAssemblies.Add(XamarinAndroid.FindAssembly("System.dll"));
            parameters.ReferencedAssemblies.Add(XamarinAndroid.FindAssembly("Mono.Android.dll"));

            var results = csc.CompileAssemblyFromSource(parameters, source);

            if (results.Errors.HasErrors)
            {
                throw new Exception(results.Errors[0].ToString());
            }

            foreach (var reference in parameters.ReferencedAssemblies)
            {
                universe.LoadFile(reference);
            }
            return universe.LoadFile(temp);
        }
    }
}
