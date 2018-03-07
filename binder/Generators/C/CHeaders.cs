using System.Linq;
using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using Embeddinator.Passes;

namespace Embeddinator.Generators
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

            WriteInclude("glib.h");
            WriteInclude("mono_embeddinator.h");
            WriteInclude("c-support.h");

            // Find dependent headers
            var referencedDecls = new GetReferencedDecls();
            Unit.Visit(referencedDecls);

            var dependencies = referencedDecls.Declarations
                .Where(d => !d.IsImplicit && d.TranslationUnit != Unit)
                .Select(d => d.TranslationUnit).Distinct();

            foreach (var dep in dependencies)
                WriteInclude($"{dep.TranslationUnit.FileNameWithoutExtension}.h");
        }

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL, "Embeddinator-4000");

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
            var getReferencedDecls = new GetReferencedDecls();
            Unit.Visit(getReferencedDecls);
            
            foreach (var decl in getReferencedDecls.Enums.Where(
                c => c.TranslationUnit == TranslationUnit && c.IsGenerated))
                    decl.Visit(this);
        }

        public override bool VisitDeclContext(DeclarationContext context)
        {
            foreach (var decl in context.Declarations.Where(d => !(d is Enumeration)))
                if (decl.IsGenerated)
                    decl.Visit(this);

            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (!VisitDeclaration(@enum))
                return false;

            PushBlock();

            var enumName = Options.GeneratorKind != GeneratorKind.CPlusPlus ?
                CGenerator.QualifiedName(@enum) : @enum.Name;
            
            Write($"typedef enum {enumName}");

            if (Options.GeneratorKind == GeneratorKind.CPlusPlus)
            {
                var typeName = CTypePrinter.VisitPrimitiveType(
                    @enum.BuiltinType.Type, new TypeQualifiers());

                if (@enum.BuiltinType.Type != PrimitiveType.Int)
                    Write($" : {typeName}");
            }

            NewLine();
            WriteStartBraceIndent();

            foreach (var item in @enum.Items)
            {
                var enumItemName = Options.GeneratorKind != GeneratorKind.CPlusPlus ?
                    $"{@enum.QualifiedName}_{item.Name}" : item.Name;

                Write(enumItemName);

                if (item.ExplicitValue)
                    Write($" = {@enum.GetItemValueAsString(item)}");

                if (item != @enum.Items.Last())
                    WriteLine(",");
            }

            NewLine();
            PopIndent();
            WriteLine($"}} {enumName};");

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!VisitDeclaration(@class))
                return false;

            PushBlock();

            VisitDeclContext(@class);

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (!VisitDeclaration(method))
                return false;

            PushBlock();

            Write("MONO_EMBEDDINATOR_API ");
            GenerateMethodSpecifier(method, method.Namespace as Class);
            WriteLine(";");

            PopBlock();

            return true;
        }
    }

}
