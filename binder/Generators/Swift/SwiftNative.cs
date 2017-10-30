using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;

namespace Embeddinator.Generators
{
    /// <summary>
    /// This class is responsible for generating JNA-compatible method and class
    /// Java code for a given managed library represented as a translation unit.
    /// </summary>
    [DebuggerDisplay("Unit = {TranslationUnit}")]
    public class SwiftNative : SwiftSources
    {
        public SwiftNative(BindingContext context, TranslationUnit unit)
            : base(context, unit)
        {
        }

        public static string GetNativeLibClassName(TranslationUnit unit) =>
            GetNativeLibClassName(unit.FileName);

        public static string GetNativeLibClassName(string fileName) =>
            $"Native_{JavaGenerator.FileNameAsIdentifier(fileName)}";

        public string ClassName => GetNativeLibClassName(TranslationUnit);

        public override string FilePath => $"{ClassName}.{FileExtension}";

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.JavaDoc, "Embeddinator-4000");

            GenerateImports();

            TranslationUnit.Visit(this);
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            Write($"public class {ClassName} ");
            WriteStartBraceIndent();

            var ret = base.VisitTranslationUnit(unit);

            WriteCloseBraceIndent();
            return ret;
        }

        public static IEnumerable<Declaration> GetOverloadedDeclarations(Declaration decl)
        {
            var @class = decl.Namespace as Class;
            return @class.Declarations.Where(d => d.OriginalName == decl.OriginalName);
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (!VisitDeclaration(method))
                return false;

            if (method.IsImplicit)
                return false;

            PushBlock(BlockKind.Method, method);

            TypePrinter.PushContext(TypePrinterContextKind.Native);

            var returnTypeName = method.ReturnType.Visit(TypePrinter);
            Write($"public static func {JavaNative.GetCMethodIdentifier(method)}(");
            Write(TypePrinter.VisitParameters(method.Parameters, hasNames: true).ToString());
            Write($") -> {returnTypeName};");

            TypePrinter.PopContext();

            PopBlock(NewLineKind.Never);
            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!VisitDeclaration(@class))
                return false;

            VisitDeclContext(@class);
            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            return VisitDeclaration(@enum);
        }
    }
}
