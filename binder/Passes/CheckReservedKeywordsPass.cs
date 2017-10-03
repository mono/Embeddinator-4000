using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;

namespace Embeddinator
{
    public class CheckReservedKeywords : TranslationUnitPass
    {
        static readonly List<string> ReservedKeywords = new List<string> {
            // C99 6.4.1: Keywords.
            "auto", "break", "case", "char", "const", "continue", "default",
            "do", "double", "else", "enum", "extern", "float", "for", "goto",
            "if", "inline", "int", "long", "register", "restrict", "return",
            "short", "signed", "sizeof", "static", "struct", "switch",
            "typedef", "union", "unsigned", "void", "volatile", "while",
            "_Alignas", "_Alignof", "_Atomic", "_Bool", "_Complex",
            "_Generic", "_Imaginary", "_Noreturn", "_Static_assert",
             "_Thread_local", "__func__", "__objc_yes", "__objc_no",
             
             // C++ 2.11p1: Keywords.
             "asm", "bool", "catch", "class", "const_cast", "delete",
             "dynamic_cast", "explicit", "export", "false", "friend",
             "mutable", "namespace", "new", "operator", "private",
             "protected", "public", "reinterpret_cast", "static_cast",
             "template", "this", "throw", "true", "try", "typename",
             "typeid", "using", "virtual", "wchar_t",

             // C++11 Keywords
             "alignas", "alignof", "char16_t", "char32_t", "constexpr",
             "decltype", "noexcept", "nullptr", "static_assert",
             "thread_local"
        };

        static readonly List<string> JavaReservedKeywords = new List<string> {
            "abstract", "assert", "boolean", "break", "byte",
            "case", "catch", "char", "class", "const",
            "continue", "default", "do", "double", "else",
            "enum", "extends", "false", "final", "finally",
            "float", "for", "goto", "if", "implements",
            "import", "instanceof", "int", "interface", "long",
            "native", "new", "null", "package",
            "private", "protected", "public", "return",
            "short", "static", "strictfp", "super", "switch",
            "synchronized", "this", "throw", "throws",
            "transient", "true", "try", "void", "volatile",
            "while"
        };

        static void CheckKeywords(IList<string> keywords, Declaration decl)
        {
            if (keywords.Contains(decl.Name))
                decl.Name = $"_{decl.Name}";
        }

        void CheckKeywords(Declaration decl)
        {
            // Ignore the "new" keyword unless we're targetting C++, because its used
            // in C as the identifier for managed constructors.
            if (decl.Name == "new" && Options.GeneratorKind != GeneratorKind.CPlusPlus)
                return;

            CheckKeywords(ReservedKeywords, decl);

            if (Options.GeneratorKind == GeneratorKind.Java)
                CheckKeywords(JavaReservedKeywords, decl);
        }

        public override bool VisitFunctionDecl(Function function)
        {
            CheckKeywords(function);
            return base.VisitFunctionDecl(function);
        }

        public override bool VisitMethodDecl(Method method)
        {
            CheckKeywords(method);
            return base.VisitMethodDecl(method);
        }

        public override bool VisitParameterDecl (Parameter parameter)
        {
            CheckKeywords(parameter);
            return true;
        }
    }
}
