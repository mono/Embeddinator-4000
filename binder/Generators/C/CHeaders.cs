using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using System.Linq;
using System;

namespace MonoEmbeddinator4000.Generators
{
    public class CHeaders : CCodeGenerator
    {
        public CHeaders(BindingContext context, TranslationUnit unit)
            : base(context, unit)
        {
        }

        public override string FileExtension => "h";

        public void WriteStandardHeader(string name)
        {
            var header = Options.GeneratorKind == GeneratorKind.CPlusPlus ?
                string.Format("c{0}", name) : string.Format("{0}.h", name);
            WriteLine("#include <{0}>", header);
        }

        public override void WriteHeaders()
        {
            WriteLine("#pragma once");
            NewLine();

            WriteInclude("mono_embeddinator.h");
        }

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);

            PushBlock();
            WriteHeaders();
            PopBlock(NewLineKind.BeforeNextBlock);

            GenerateDefines();

            PushBlock();
            WriteLine("MONO_EMBEDDINATOR_BEGIN_DECLS");
            PopBlock(NewLineKind.BeforeNextBlock);

            WriteForwardDecls();

            VisitDeclContext(Unit);

            PushBlock();
            WriteLine("MONO_EMBEDDINATOR_END_DECLS");
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateDefines()
        {
            PushBlock();

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public virtual void WriteForwardDecls()
        {
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            PushBlock();

            Write("typedef enum {0}", @enum.Name);

            if (Options.GeneratorKind == GeneratorKind.CPlusPlus)
            {
                var typePrinter = CTypePrinter;
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
            WriteLine("}} {0};", @enum.Name);

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            PushBlock();

            VisitDeclContext(@class);

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public override bool VisitMethodDecl(Method method)
        {
            PushBlock();

            GenerateMethodSignature(method, isSource: false);
            WriteLine(";");

            PopBlock();

            return true;
        }
    }

}
