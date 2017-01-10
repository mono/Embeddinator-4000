using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using System.Linq;

namespace MonoEmbeddinator4000.Generators
{
    public class ObjCSources : CSources
    {
        public ObjCSources(BindingContext context, Options options,
            TranslationUnit unit)
            : base(context, options, unit)
        {
        }

        public override string FileExtension
        {
            get { return "mm"; }
        }

        public override void Process()
        {
            base.Process();
        }
        
        public override void GenerateMethodSignature(Method method,
            bool isSource)
        {
            this.GenerateObjCMethodSignature(method);
        }
        
        public override bool VisitClassDecl(Class @class)
        {
            PushBlock();

            WriteLine($"@implementation {@class.QualifiedName}");
            NewLine();

            VisitDeclContext(@class);

            NewLine();
            WriteLine("@end");

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }
    }
}
