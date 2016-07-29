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

            PushBlock();
            WriteLine("#pragma once");
            NewLine();
            WriteLine("#include <stdbool.h>");
            WriteLine("#include <stdint.h>");
            PopBlock(NewLineKind.BeforeNextBlock);

            GenerateDefines();

            PushBlock();
            WriteLine("MONO_BEGIN_DECLS");
            PopBlock(NewLineKind.BeforeNextBlock);

            VisitDeclContext(Unit);

            PushBlock();
            WriteLine("MONO_END_DECLS");
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateDefines()
        {
            PushBlock();

            WriteLine("#ifdef  __cplusplus");
            WriteLineIndent("#define MONO_BEGIN_DECLS  extern \"C\" {");
            WriteLineIndent("#define MONO_END_DECLS    }");
            WriteLine("#else");
            WriteLineIndent("#define MONO_BEGIN_DECLS");
            WriteLineIndent("#define MONO_END_DECLS");
            WriteLine("#endif");
            NewLine();

            WriteLine("#if defined(_MSC_VER)");
            WriteLineIndent("#define MONO_API_EXPORT __declspec(dllexport)");
            WriteLineIndent("#define MONO_API_IMPORT __declspec(dllimport)");
            WriteLine("#else");
            WriteLineIndent("#define MONO_API_EXPORT __attribute__ ((visibility (\"default\")))");
            WriteLineIndent("#define MONO_API_IMPORT");
            WriteLine("#endif");
            NewLine();

            WriteLine("#if defined(MONO_DLL_EXPORT)");
            WriteLineIndent("#define MONO_API MONO_API_EXPORT");
            WriteLine("#else");
            WriteLineIndent("#define MONO_API MONO_API_IMPORT");
            WriteLine("#endif");

            PopBlock(NewLineKind.BeforeNextBlock);
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
