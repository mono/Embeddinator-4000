using System.Collections.Generic;
using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.CSharp;

namespace MonoEmbeddinator4000.Generators
{
    public class JavaSources : CSharpSources
    {
        public JavaSources(BindingContext context, TranslationUnit unit)
            : base(context, new List<TranslationUnit> { unit })
        {
            TypePrinter = new JavaTypePrinter(context);
        }

        public override string FileExtension => "java";

        public string AssemblyId => CGenerator.AssemblyId(TranslationUnit);

        public override void Process()
        {
            GenerateFilePreamble();

            PushBlock();
            PopBlock(NewLineKind.BeforeNextBlock);

            TranslationUnit.Visit(this);
        }

        public override bool VisitDeclContext(DeclarationContext context)
        {
            foreach (var decl in context.Declarations)
                decl.Visit(this);


            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            VisitDeclContext(@class);

            return true;
        }

        public override bool VisitMethodDecl(Method method)
        {
            return true;
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            return true;
        }

        public override bool VisitProperty(Property property)
        {
            // Ignore properties since they're converted to getter/setter pais.
            return true;
        }
    }
}
