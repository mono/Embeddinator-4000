using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Passes;

namespace MonoEmbeddinator4000
{
    public class CheckKeywordsPass : TranslationUnitPass
    {
        readonly List<string> ReservedKeywords = new List<string> {
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

        public override bool VisitParameterDecl (Parameter parameter)
        {
            if (ReservedKeywords.Contains(parameter.Name))
                parameter.Name = string.Format("_{0}", parameter.Name);

            return true;
        }
    }
}
