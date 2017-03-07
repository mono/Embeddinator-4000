using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;

using MonoEmbeddinator4000.Passes;

namespace MonoEmbeddinator4000.Generators
{
    public class JavaGenerator : Generator
    {
        public JavaGenerator(BindingContext context) : base(context)
        {
        }

        List<CodeGenerator> Generators = new List<CodeGenerator>();

        public override List<CodeGenerator> Generate(IEnumerable<TranslationUnit> units)
        {
            var unit = units.First();

            // Java packages work very differently from C++/C# namespaces, so we take a
            // different approach. We generate a file for each declaration in the source.
            GenerateDeclarationContext(unit);

            return Generators;
        }

        public void GenerateDeclarationContext(DeclarationContext context)
        {
            foreach (var decl in context.Declarations)
            {
                if (decl is Method || decl is Field || decl is Property ||
                    decl is TypedefDecl) continue;

                if (!(decl is Namespace))
                {
                    var sources = new JavaSources(Context, decl);
                    Generators.Add(sources);
                }

                if (decl is DeclarationContext)
                    GenerateDeclarationContext(decl as DeclarationContext);
            }
        }

        public override bool SetupPasses()
        {
            Context.TranslationUnitPasses.AddPass(new PropertyToGetterSetterPass());
            Context.TranslationUnitPasses.RenameDeclsLowerCase(
                RenameTargets.Function | RenameTargets.Method | RenameTargets.Property);
            return true;
        }

        public JavaTypePrinter TypePrinter => new JavaTypePrinter(Context);

        protected override string TypePrinterDelegate(CppSharp.AST.Type type)
        {
            return type.Visit(TypePrinter).ToString();
        }
    }
}
