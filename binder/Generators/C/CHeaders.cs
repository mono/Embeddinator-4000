using CppSharp.AST;

namespace MonoManagedToNative.Generators
{
    public class CHeaders : CTemplate
    {
        public CHeaders(Driver driver, TranslationUnit unit) : base(driver, unit)
        {
        }

        public override string FileExtension
        {
            get { return "h"; }
        }

        public override void Process()
        {
            GenerateFilePreamble();

            PushBlock(CBlockKind.Includes);
            WriteLine("#pragma once");
            WriteLine("#include <stdbool.h>");
            PopBlock(NewLineKind.BeforeNextBlock);

            VisitDeclContext(Unit);
        }

        public override bool VisitClassDecl(Class @class)
        {
            PushBlock(CBlockKind.Class);

            PushBlock();
            WriteLine("typedef struct {0} {0};", @class.Name);
            PopBlock(NewLineKind.BeforeNextBlock);

            VisitDeclContext(@class);

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public override bool VisitMethodDecl(Method method)
        {
            PushBlock(CBlockKind.Function);

            GenerateMethodSignature(method);
            WriteLine(";");

            PopBlock();

            return true;
        }
    }

}
