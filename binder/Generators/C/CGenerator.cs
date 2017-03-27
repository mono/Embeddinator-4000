using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;

namespace MonoEmbeddinator4000.Generators
{
    public class CGenerator : Generator
    {
        public CGenerator(BindingContext context) : base(context)
        {
        }

        public override List<CodeGenerator> Generate(IEnumerable<TranslationUnit> units)
        {
            var unit = units.First();
            var headers = new CHeaders(Context, unit);
            var sources = new CSources(Context, unit);

            return new List<CodeGenerator> { headers, sources };
        }

        public static string GenId(string id)
        {
            return "__" + id;
        }

        public static string ObjectInstanceId => GenId("object");

        public static string AssemblyId(TranslationUnit unit)
        {
            return GenId(unit.FileName).Replace('.', '_');
        }

        private static CppTypePrintFlavorKind GetTypePrinterFlavorKind(GeneratorKind kind)
        {
            switch (kind)
            {
                case GeneratorKind.C:
                    return CppTypePrintFlavorKind.C;
                case GeneratorKind.CPlusPlus:
                    return CppTypePrintFlavorKind.Cpp;
                case GeneratorKind.ObjectiveC:
                    return CppTypePrintFlavorKind.ObjC;
            }

            throw new NotImplementedException();
        }

        public static CManagedToNativeTypePrinter GetCTypePrinter(GeneratorKind kind)
        {
            var typePrinter = new CManagedToNativeTypePrinter
            {
                PrintScopeKind = TypePrintScopeKind.Qualified,
                PrintFlavorKind = GetTypePrinterFlavorKind(kind),
                PrintVariableArrayAsPointers = true
            };

            return typePrinter;
        }

        public virtual CManagedToNativeTypePrinter TypePrinter =>
            GetCTypePrinter(GeneratorKind.C);

        public override bool SetupPasses()
        {
            return true;
        }

        protected override string TypePrinterDelegate(CppSharp.AST.Type type)
        {
            return type.Visit(TypePrinter);
        }
    }

    public abstract class CCodeGenerator : CodeGenerator
    {
        public TranslationUnit Unit;

        public CCodeGenerator(BindingContext context,
            TranslationUnit unit) : base(context, unit)
        {
            Unit = unit;
        }

        public override string GeneratedIdentifier(string id)
        {
            return CGenerator.GenId(id);
        }

        public string QualifiedName(Declaration decl)
        {
            if (Options.GeneratorKind == GeneratorKind.CPlusPlus)
                return decl.Name;

            return decl.QualifiedName;
        }

        public CManagedToNativeTypePrinter CTypePrinter =>
                CGenerator.GetCTypePrinter(Options.GeneratorKind);

        public virtual void WriteHeaders() { }

        public void WriteInclude(string include)
        {
            if (Options.GenerateSupportFiles)
                WriteLine("#include \"{0}\"", include);
            else
                WriteLine("#include <{0}>", include);
        }

        public virtual void GenerateMethodSignature(Method method, bool isSource = true)
        {
            var @class = method.Namespace as Class;
            var retType = method.ReturnType.Visit(CTypePrinter);

            Write("{0}{1} {2}_{3}(", isSource ? string.Empty : "MONO_EMBEDDINATOR_API ",
                retType, @class.QualifiedName, method.Name);

            Write(CTypePrinter.VisitParameters(method.Parameters));

            Write(")");
        }

        public virtual string GenerateClassObjectAlloc(string type)
        {
            return $"({type}*) calloc(1, sizeof({type}))";
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            PushBlock();

            var typeName = typedef.Type.Visit(CTypePrinter);
            WriteLine("typedef {0} {1};", typeName, typedef.Name);

            var newlineKind = NewLineKind.BeforeNextBlock;

            var declarations = typedef.Namespace.Declarations;
            var newIndex = declarations.FindIndex(d => d == typedef) + 1;
            if (newIndex < declarations.Count)
            {
                if (declarations[newIndex] is TypedefDecl)
                    newlineKind = NewLineKind.Never;
            }

            PopBlock(newlineKind);

            return true;
        }

        public override bool VisitFieldDecl(Field field)
        {
            WriteLine("{0} {1};", field.Type, field.Name);
            return true;
        }

        public override bool VisitProperty(Property property)
        {
            return true;
        }
    }
}
