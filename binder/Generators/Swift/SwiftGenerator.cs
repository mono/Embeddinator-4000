using System.Collections.Generic;
using System.Linq;
using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;

namespace Embeddinator.Generators
{
    public class SwiftGenerator : Generator
    {
        public SwiftTypePrinter TypePrinter { get; internal set; }

        public static string IntPtrType = "UnsafePointer<Void>";

        PassBuilder<TranslationUnitPass> Passes;

        public SwiftGenerator(BindingContext context)
            : base(context)
        {
            TypePrinter = new SwiftTypePrinter(Context);

            Passes = new PassBuilder<TranslationUnitPass>(Context);
            CGenerator.SetupPasses(Passes);
        }

        public override List<CodeGenerator> Generate(IEnumerable<TranslationUnit> units)
        {
            var unit = units.First();
            var sources = new SwiftSources(Context, unit);

            // Also generate a separate file with equivalent of P/Invoke declarations.
            var nativeSources = GenerateNativeDeclarations(unit);

            return new List<CodeGenerator> { sources, nativeSources };
        }

        public CodeGenerator GenerateNativeDeclarations(TranslationUnit unit)
        {
            CGenerator.RunPasses(Context, Passes);
            return new SwiftNative(Context, unit);
        }

        public override bool SetupPasses()
        {
            return true;
        }

        protected override string TypePrinterDelegate(CppSharp.AST.Type type)
        {
            return type.Visit(TypePrinter).Type;
        }
    }
}
