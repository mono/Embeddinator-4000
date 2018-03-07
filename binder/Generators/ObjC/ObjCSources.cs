using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;

namespace Embeddinator.Generators
{
    public class ObjCSources : CSources
    {
        public ObjCSources(BindingContext context, TranslationUnit unit)
            : base(context, unit)
        {
        }

        public override string FileExtension => "mm";

        public override void Process()
        {
            base.Process();
        }
        
        public override void GenerateMethodSpecifier(Method method, Class @class)
        {
            this.GenerateObjCMethodSignature(method);
        }

        public override string GenerateClassObjectAlloc(Declaration decl)
        {
            return $"[[{decl.Visit(CTypePrinter)} alloc]init]";
        }

        public override bool VisitClassDecl(Class @class)
        {
            PushBlock();

            GenerateClassLookup(@class);

            WriteLine($"@implementation {@class.QualifiedName}");
            NewLine();

            VisitDeclContext(@class);

            NewLine();
            WriteLine("@end");

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public override bool VisitFieldDecl(Field field)
        {
            return this.GenerateObjCField(field);
        }
    }
}
