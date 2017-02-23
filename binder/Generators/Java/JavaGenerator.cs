using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
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
                if (decl is Method || decl is Field || decl is Property) continue;

                if (!(decl is Namespace))
                {
                    var sources = new JavaSources(Context, decl);
                    Generators.Add(sources);
                }

                if (decl is DeclarationContext)
                    GenerateDeclarationContext(decl as DeclarationContext);
            }
        }

        public static JavaManagedToNativeTypePrinter GetJavaManagedToNativeTypePrinter()
        {
            return new JavaManagedToNativeTypePrinter
            {
                PrintScopeKind = TypePrintScopeKind.Qualified,
                PrintVariableArrayAsPointers = true
            };
        }

        public override bool SetupPasses()
        {
            Context.TranslationUnitPasses.AddPass(new PropertyToGetterSetterPass());
            return true;
        }

        protected override string TypePrinterDelegate(CppSharp.AST.Type type)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class JavaCodeGenerator : CodeGenerator
    {
        public TranslationUnit Unit;

        public JavaCodeGenerator(BindingContext context, TranslationUnit unit)
            : base(context, unit)
        {
            Unit = unit;
        }

        public CManagedToNativeTypePrinter CTypePrinter =>
                CGenerator.GetCTypePrinter(Options.GeneratorKind);

        public virtual void GenerateMethodSignature(Method method, bool isSource = true)
        {
            var @class = method.Namespace as Class;
            var retType = method.ReturnType.Visit(CTypePrinter);

            Write("{0}{1} {2}_{3}(", isSource ? string.Empty : "MONO_M2N_API ",
                retType, @class.QualifiedName, method.Name);

            Write(CTypePrinter.VisitParameters(method.Parameters));

            Write(")");
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            return true;
        }

        public override bool VisitNamespace (Namespace @namespace)
        {
            return VisitDeclContext(@namespace);
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
