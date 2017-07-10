﻿using System;
using System.CodeDom.Compiler;
using System.IO;
using IKVM.Reflection;
using Microsoft.CSharp;
using NUnit.Framework;

namespace MonoEmbeddinator4000.Tests
{
    /// <summary>
    /// These are a set of integration/approval tests validating that we are getting expected C# code from ResourceDesignerGenerator
    /// </summary>
    [TestFixture]
    public class ResourceDesignerTest
    {
        ResourceDesignerGenerator generator;
        Universe universe;

        [SetUp]
        public void SetUp()
        {
            universe = new Universe();

            generator = new ResourceDesignerGenerator
            {
                OutputDirectory = Environment.CurrentDirectory,
                PackageName = "com.mono.embeddinator",
            };
        }

        [TearDown]
        public void TearDown()
        {
            //Locks files on Windows
            universe.Dispose();

            //Temp file
            File.Delete(generator.MainAssembly);
        }

        Assembly LoadAssembly(string resourceFile)
        {
            var temp = Path.GetTempFileName();
            var parameters = new CompilerParameters
            {
                OutputAssembly = temp,
            };
            parameters.ReferencedAssemblies.Add(XamarinAndroid.FindAssembly("System.dll"));
            parameters.ReferencedAssemblies.Add(XamarinAndroid.FindAssembly("Mono.Android.dll"));

            AssemblyGenerator.CreateFromResource(resourceFile, parameters);

            foreach (var reference in parameters.ReferencedAssemblies)
            {
                universe.LoadFile(reference);
            }
            return universe.LoadFile(temp);
        }

        void LoadAndGenerate(string resourceFile)
        {
            var assembly = LoadAssembly(resourceFile);
            generator.Assemblies = new[] { assembly };
            generator.MainAssembly = assembly.Location;
            generator.Generate();
        }

        [Test]
        public void String()
        {
            LoadAndGenerate("String");

            string source = generator.ToSource();
            Approvals.Verify(source);
        }

        [Test]
        public void Full()
        {
            LoadAndGenerate("Full");

            string source = generator.ToSource();
            Approvals.Verify(source);
        }
    }
}
