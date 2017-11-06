using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;

namespace Embeddinator.Generators
{
    public class SwiftGenerator : Generator
    {
        public SwiftTypePrinter TypePrinter { get; internal set; }

        public static string IntPtrType = "UnsafeRawPointer";

        public SwiftGenerator(BindingContext context)
            : base(context)
        {
            TypePrinter = new SwiftTypePrinter(Context);
        }

        public override List<CodeGenerator> Generate(IEnumerable<TranslationUnit> units)
        {
            var unit = units.First();
            var sources = new SwiftSources(Context, unit);

            return new List<CodeGenerator> { sources };
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
