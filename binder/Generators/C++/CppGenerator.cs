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
    public class CppGenerator : CGenerator
    {
        public CppGenerator(BindingContext context) : base(context)
        {
            Context.Options.GeneratorKind = GeneratorKind.CPlusPlus;
        }

        public override List<CodeGenerator> Generate(IEnumerable<TranslationUnit> units)
        {
            var unit = units.First();
            var headers = new CppHeaders(Context, unit);
            var sources = new CppSources(Context, unit);

            return new List<CodeGenerator> { headers, sources };
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

        public static CppManagedToNativeTypePrinter GetCppTypePrinter(GeneratorKind kind)
        {
            var typePrinter = new CppManagedToNativeTypePrinter
            {
                PrintScopeKind = TypePrintScopeKind.Qualified,
                PrintFlavorKind = GetTypePrinterFlavorKind(kind),
                PrintVariableArrayAsPointers = true
            };

            return typePrinter;
        }

        public CppTypePrinter TypePrinter =>
            GetCppTypePrinter(GeneratorKind.CPlusPlus);

        public override bool SetupPasses()
        {
            SetupPasses(Context.TranslationUnitPasses);
            return true;
        }

        public static void SetupPasses(PassBuilder<TranslationUnitPass> passes)
        {
            passes.AddPass(new FixMethodParametersPass());
        }

        protected override string TypePrinterDelegate(CppSharp.AST.Type type)
        {
            return type.Visit(TypePrinter);
        }
    }

    public abstract class CppCodeGenerator : CodeGenerator
    {
        public TranslationUnit Unit;

        Options EmbedOptions => Context.Options as Options; 

        public CppCodeGenerator(BindingContext context,
            TranslationUnit unit) : base(context, unit)
        {
            Unit = unit;
        }

        public override string GeneratedIdentifier(string id)
        {
            return CppGenerator.GenId(id);
        }

        public string QualifiedName(Declaration decl)
        {
            if (Options.GeneratorKind == GeneratorKind.CPlusPlus)
                return decl.Name;

            return decl.QualifiedName;
        }

        public CppTypePrinter CppTypePrinter =>
                CppGenerator.GetCppTypePrinter(Options.GeneratorKind);

        public virtual void WriteHeaders() { }

        public void WriteInclude(string include)
        {
            if (EmbedOptions.GenerateSupportFiles)
                WriteLine("#include \"{0}\"", include);
            else
                WriteLine("#include <{0}>", include);
        }

        public static string GetMethodIdentifier(Method method)
        {
            var @class = method.Namespace as Class;
            return $"{method.Name}";
        }

        public override void GenerateMethodSpecifier(Method method, Class @class)
        {
            var methodName = string.Empty;
            if (method.IsConstructor)
            {
                methodName = @class.Name;
                Write($"{method.Namespace}::{methodName}(");
            }
            else
            {
                methodName = GetMethodIdentifier(method);
                var retType = method.ReturnType.Visit(CppTypePrinter);
                Write($"{retType} {@class.Name}::{methodName}(");
            }
            Write(CppTypePrinter.VisitParameters(method.Parameters));
            Write(")");
        }

        public virtual string GenerateClassObjectAlloc(string type)
        {
            return $"new {type}()";
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            PushBlock();

            var typeName = typedef.Type.Visit(CppTypePrinter);
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
            return true;
        }
    }
}
