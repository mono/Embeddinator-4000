using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using MonoEmbeddinator4000.Passes;
using System.Linq;
 
namespace MonoEmbeddinator4000.Generators
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
            var refs = new GetReferencedClasses();
            TranslationUnit.Visit(refs);

            var classes = refs.Classes.ToList();
            classes.RemoveAll((c) => c == GenerateArrayTypes.MonoEmbedArray);
            classes.RemoveAll((c) => c == GenerateObjectTypesPass.MonoEmbedObject);

            PushBlock();

            foreach (var @class in classes.Distinct())
                WriteLine($"@class {@class.QualifiedName};");

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public override void GenerateMethodSignature(Method method,
            bool isSource)
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
