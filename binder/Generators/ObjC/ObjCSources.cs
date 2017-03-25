using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;

namespace MonoEmbeddinator4000.Generators
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
        
        public override void GenerateMethodSignature(Method method,
            bool isSource)
        {
            this.GenerateObjCMethodSignature(method, headers: false);
        }

        public override string GenerateClassObjectAlloc(string type)
        {
            return $"[[{type} alloc]init]";
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
