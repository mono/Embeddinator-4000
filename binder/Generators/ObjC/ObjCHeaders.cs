using CppSharp.AST;
using CppSharp.Generators;

namespace MonoEmbeddinator4000.Generators
{
    public class ObjCHeaders : CHeaders
    {
        public ObjCHeaders(BindingContext context, Options options,
            TranslationUnit unit)
            : base(context, options, unit)
        {
        }

        public override void Process()
        {
            base.Process();
        }

        public override void WriteHeaders()
        {
            base.WriteHeaders();
            WriteLine("#import <Foundation/Foundation.h>");
        }

        public override void GenerateMethodSignature(Method method,
            bool isSource)
        {
            this.GenerateObjCMethodSignature(method);
        }

        public override bool VisitClassDecl(Class @class)
        {
            PushBlock();

            WriteLine("@interface {0} : NSObject", @class.QualifiedName);
            NewLine();

            VisitDeclContext(@class);

            NewLine();
            WriteLine("@end");

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }
    }
}
