using CppSharp.AST;
using CppSharp.Generators;
using System.Linq;

namespace MonoManagedToNative.Generators
{
    public class CHeaders : CTemplate
    {
        public CHeaders(BindingContext context, Options options, TranslationUnit unit)
         : base(context, options, unit)
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
            WriteLine("#include <mono_managed_to_native.h>");
            PopBlock(NewLineKind.BeforeNextBlock);

            GenerateDefines();

            PushBlock();
            WriteLine("MONO_M2N_BEGIN_DECLS");
            PopBlock(NewLineKind.BeforeNextBlock);

            VisitDeclContext(Unit);

            PushBlock();
            WriteLine("MONO_M2N_END_DECLS");
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateDefines()
        {
            PushBlock();

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            PushBlock();

            Write("enum {0}", @enum.Name);

            if (Options.Language == GeneratorKind.CPlusPlus)
            {
                var typePrinter = new CppTypePrinter();
                var typeName = typePrinter.VisitPrimitiveType(
                    @enum.BuiltinType.Type, new TypeQualifiers());

                if (@enum.BuiltinType.Type != PrimitiveType.Int)
                    Write(" : {0}", typeName);
            }

            NewLine();
            WriteStartBraceIndent();

            foreach (var item in @enum.Items)
            {
                Write(string.Format("{0}", item.Name));

                if (item.ExplicitValue)
                    Write(string.Format(" = {0}", @enum.GetItemValueAsString(item)));

                if (item != @enum.Items.Last())
                    WriteLine(",");
            }

            NewLine();
            PopIndent();
            WriteLine("};");

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            PushBlock(CBlockKind.Class);

            PushBlock();
            WriteLine("typedef struct {0} {0};", @class.QualifiedName);
            PopBlock(NewLineKind.BeforeNextBlock);

            VisitDeclContext(@class);

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public override bool VisitMethodDecl(Method method)
        {
            PushBlock(CBlockKind.Function);

            GenerateMethodSignature(method, isSource: false);
            WriteLine(";");

            PopBlock();

            return true;
        }
    }

}
