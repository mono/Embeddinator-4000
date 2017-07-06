﻿using System;
using System.CodeDom.Compiler;
using System.IO;
using IKVM.Reflection;
using Microsoft.CSharp;

namespace MonoEmbeddinator4000.Tests
{
    public static class Samples
    {
        public static Assembly LoadFile(string resourceFile)
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
            var monoDroidPath = XamarinAndroid.Path;
            parameters.ReferencedAssemblies.Add(Path.Combine(monoDroidPath, "lib", "xbuild-frameworks", "MonoAndroid", "v1.0", "System.dll"));
            parameters.ReferencedAssemblies.Add(Path.Combine(monoDroidPath, "lib", "xbuild-frameworks", "MonoAndroid", XamarinAndroid.TargetFrameworkVersion, "Mono.Android.dll"));

            var results = csc.CompileAssemblyFromSource(parameters, source);

            if (results.Errors.HasErrors)
            {
                throw new Exception(results.Errors[0].ToString());
            }

            var u = new Universe();
            foreach (var reference in parameters.ReferencedAssemblies)
            {
                u.LoadFile(reference);
            }
            return u.LoadFile(temp);
        }
    }
}
