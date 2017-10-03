using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using Embeddinator.Passes;
using System.Linq;
 
namespace Embeddinator.Generators
{
    public class ObjCHeaders : CHeaders
    {
        public ObjCHeaders(BindingContext context, TranslationUnit unit)
            : base(context, unit)
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

        public override void WriteForwardDecls()
        {
            var refs = new GetReferencedDecls();
            TranslationUnit.Visit(refs);

            var classes = refs.Classes.ToList();
            classes.RemoveAll((c) => c == GenerateArrayTypes.MonoEmbedArray);
            classes.RemoveAll((c) => c == GenerateObjectTypesPass.MonoEmbedObject);

            PushBlock();

            foreach (var @class in classes.Distinct())
                WriteLine($"@class {@class.QualifiedName};");

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public override void GenerateMethodSpecifier(Method method, Class @class)
        {
            this.GenerateObjCMethodSignature(method);
        }

        public override bool VisitClassDecl(Class @class)
        {
            PushBlock();

            Write("@interface {0} : NSObject", @class.QualifiedName);

            var hasFields = @class.Fields.Count != 0;
            if (hasFields)
            {
                Write(" ");
                WriteStartBraceIndent();

                foreach (var field in @class.Fields)
                    field.Visit(this);

                WriteCloseBraceIndent();
                NewLine();
            }
            
            VisitDeclContext(@class);

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
