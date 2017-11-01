﻿using System.Collections.Generic;
using System.IO;
using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;

using Embeddinator.Passes;

namespace Embeddinator.Generators
{
    public class JavaGenerator : Generator
    {
        public static string IntPtrType = "com.sun.jna.Pointer";

        public static string GetNativeLibPackageName(TranslationUnit unit) =>
            GetNativeLibPackageName(unit.FileName);

        public static string GetNativeLibPackageName(string fileName) =>
            FileNameAsIdentifier(fileName).ToLowerInvariant();

        public static string FileNameAsIdentifier(string fileName) =>
            Path.GetFileNameWithoutExtension(fileName).Replace('.', '_').Replace('-', '_');

        public JavaTypePrinter TypePrinter;

        PassBuilder<TranslationUnitPass> Passes;

        public JavaGenerator(BindingContext context) : base(context)
        {
            TypePrinter = new JavaTypePrinter(Context);

            Passes = new PassBuilder<TranslationUnitPass>(Context);
            CGenerator.SetupPasses(Passes);
        }

        public override List<CodeGenerator> Generate(IEnumerable<TranslationUnit> units)
        {
            var generators = new List<CodeGenerator>();

            // Java packages work very differently from C++/C# namespaces, so we take a
            // different approach. We generate a file for each declaration in the source.
            foreach (var unit in units)
            {
                GenerateDeclarationContext(generators, unit);

                //NOTE: we should skip over Mono.Android and Java.Interop
                if (unit.FileName != "Mono.Android.dll" && unit.FileName != "Java.Interop.dll")
                {
                    // Also generate a separate file with equivalent of P/Invoke declarations
                    // for JNA.
                    GenerateNativeDeclarations(generators, unit);
                }
            }

            return generators;
        }

        public void GenerateNativeDeclarations(List<CodeGenerator> generators,
            TranslationUnit unit)
        {
            CGenerator.RunPasses(Context, Passes);
            generators.Add(new JavaNative(Context, unit));
        }

        public void GenerateDeclarationContext(List<CodeGenerator> generators,
            DeclarationContext context)
        {
            foreach (var decl in context.Declarations)
            {
                if (decl is Method || decl is Field || decl is Property ||
                    decl is TypedefDecl) continue;

                if (!decl.IsGenerated) continue;

                if (!(decl is Namespace))
                {
                    var sources = new JavaSources(Context, decl);
                    generators.Add(sources);
                }

                if (decl is DeclarationContext)
                    GenerateDeclarationContext(generators, decl as DeclarationContext);
            }
        }

        public override bool SetupPasses()
        {
            Context.TranslationUnitPasses.AddPass(new PropertyToGetterSetterPass());
            Context.TranslationUnitPasses.RenameDeclsLowerCase(
                RenameTargets.Function | RenameTargets.Method | RenameTargets.Property);
            Context.TranslationUnitPasses.AddPass(new InterfacesPass());
            return true;
        }

        protected override string TypePrinterDelegate(CppSharp.AST.Type type)
        {
            return type.Visit(TypePrinter).ToString();
        }
    }
}
