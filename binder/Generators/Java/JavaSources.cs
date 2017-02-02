using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;

namespace MonoEmbeddinator4000.Generators
{
    public class JavaSources : CTemplate
    {
        public JavaSources(BindingContext context, TranslationUnit unit)
            : base(context, unit)
        {
        }

        public override string FileExtension => "java";

        public string AssemblyId => CGenerator.AssemblyId(Unit);

        public override void Process()
        {
            GenerateFilePreamble();

            PushBlock();
            PopBlock(NewLineKind.BeforeNextBlock);

            VisitDeclContext(Unit);
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
    }
}
