using CppSharp.AST;
using CppSharp.Generators;
using System.Linq;

namespace MonoManagedToNative.Generators
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

        public override bool VisitClassDecl(Class @class)
        {
            PushBlock(CBlockKind.Class);

            WriteLine("@interface {0} : NSObject", @class.QualifiedName);
            WriteStartBraceIndent();

            WriteCloseBraceIndent();
            NewLine();

            //VisitDeclContext(@class);

            WriteLine("@end");

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }        
    }

}
