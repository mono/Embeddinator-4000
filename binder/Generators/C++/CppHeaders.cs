using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using System.Linq;
using System;
using System.Collections.Generic;

namespace MonoEmbeddinator4000.Generators
{
    public class CppHeaders : CppCodeGenerator
    {
        public CppHeaders(BindingContext context, TranslationUnit unit)
            : base(context, unit)
        {
        }

        public override string FileExtension => "hpp";

        public void WriteStandardHeader(string name)
        {
            var header = string.Format("{0}.h", name);
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
            GenerateFilePreamble(CommentKind.BCPL, "Embeddinator-4000");

            PushBlock();
            WriteHeaders();
            PopBlock(NewLineKind.BeforeNextBlock);

            GenerateDefines();

            PushBlock();
            WriteLine("MONO_EMBEDDINATOR_BEGIN_DECLS");
            PopBlock(NewLineKind.BeforeNextBlock);
            PushIndent();

            WriteForwardDecls(Unit);

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

        public virtual void WriteForwardDecls(TranslationUnit unit)
        {
            GenerateClassForwardDecl(unit);
        }

        public void GenerateClassForwardDecl(TranslationUnit unit)
        {
            PushBlock();
            foreach (var @class in unit.Classes)
            {
                WriteLine($"class {@class.Name};");
            }
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public override bool VisitDeclContext(DeclarationContext context)
        {
            foreach (var decl in context.Enums)
                if (decl.IsGenerated)
                    decl.Visit(this);

            foreach (var decl in context.Declarations.Where(d => !(d is Enumeration)))
                if (decl.IsGenerated)
                    decl.Visit(this);

            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            PushBlock();

            var enumName = @enum.Name;
            
            Write($"enum class {enumName}");

            NewLine();
            WriteStartBraceIndent();

            foreach (var item in @enum.Items)
            {
                var enumItemName = $"{item.Name}";

                Write(enumItemName);

                if (item.ExplicitValue)
                    Write($" = {@enum.GetItemValueAsString(item)}");

                if (item != @enum.Items.Last())
                    WriteLine(",");
            }

            NewLine();
            PopIndent();
            WriteLine("};");

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public override void GenerateClassSpecifier(Class @class)
        {
            var keywords = new List<string>();
            keywords.Add("MONO_EMBEDDINATOR_API");
            keywords.Add(@class.IsValueType ? "struct" : "class");
            keywords.Add(SafeIdentifier(@class.Name));
            Write(string.Join(" ", keywords));

            base.GenerateClassSpecifier(@class);
        }

        public void GenerateOperator(Method method)
        {
            PushBlock(BlockKind.Method, method);
            // ignore them
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateMethod(Method method)
        {
            if (ASTUtils.CheckIgnoreMethod(method, Options)) return;

            PushBlock(BlockKind.Method, method);
            GenerateDeclarationCommon(method);

            GenerateMethodSpecifier(method, method.Namespace as Class);
            WriteLine(";");

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateClassConstructors(Class @class)
        {
            if (@class.IsStatic)
                return;

            foreach (var ctor in @class.Constructors)
            {
                if (ASTUtils.CheckIgnoreMethod(ctor, Options))
                    continue;

                GenerateMethod(ctor);
            }
        }

        public void GenerateClassMethods(List<Method> methods)
        {
            if (methods.Count == 0)
                return;

            PushIndent();

            var @class = (Class)methods[0].Namespace;

            if (@class.IsValueType)
                foreach (var @base in @class.Bases.Where(b => b.IsClass && !b.Class.Ignore))
                    GenerateClassMethods(@base.Class.Methods.Where(m => !m.IsOperator).ToList());

            var staticMethods = new List<Method>();
            foreach (var method in methods)
            {
                if (ASTUtils.CheckIgnoreMethod(method, Options))
                    continue;

                if (method.IsConstructor)
                    continue;

                if (method.IsStatic)
                {
                    staticMethods.Add(method);
                    continue;
                }

                GenerateMethod(method);
            }

            NeedNewLine();

            foreach (var method in staticMethods)
                GenerateMethod(method);

            PopIndent();
        }

        public void GenerateClassFields(Class @class)
        {
            WriteLine("private:");
            WriteLine($"MonoEmbedObject* __object;");
            WriteLine("public:");
            WriteLine($"static MonoClass* class_{@class.Name};");
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (@class.IsIncomplete || @class.IsAbstract || @class.IsInjected)
                return true;
            PushBlock(BlockKind.Class);

            GenerateClassSpecifier(@class);

            NewLine();
            WriteStartBraceIndent();

            GenerateClassFields(@class);
            GenerateClassConstructors(@class);
            GenerateClassMethods(@class.Methods);

            VisitDeclContext(@class);

            PopIndent();
            WriteLine("};");
            PopBlock(NewLineKind.BeforeNextBlock);
            return true;
        }

        public override bool VisitMethodDecl(Method method)
        {
            GenerateMethod(method);
            //Write("MONO_EMBEDDINATOR_API ");
            //GenerateMethodSpecifier(method, method.Namespace as Class);
            //WriteLine(";");

            return true;
        }

        public override bool VisitProperty(Property property)
        {
            if (property.Field == null)
                return false;

            var getter = property.GetMethod;
            VisitMethodDecl(getter);

            var setter = property.SetMethod;
            VisitMethodDecl(setter);

            return true;
        }
    }

}
