using System.Collections.Generic;
using System.IO;
using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;

namespace MonoEmbeddinator4000.Generators
{
    /// <summary>
    /// This class is responsible for generating JNA-compatible method and class
    /// Java code for a given managed library represented as a translation unit.
    /// </summary>
    public class JavaNative : JavaSources
    {
        public JavaNative(BindingContext context, TranslationUnit unit)
            : base(context, unit)
        {
        }

        public static string GetNativeLibClassName(TranslationUnit unit) =>
            $"Native_{unit.FileName.Replace('.', '_')}";

        public string ClassName => GetNativeLibClassName(TranslationUnit);

        public override string FilePath
        {
            get
            {
                var names = new List<string>
                {
                    JavaGenerator.GetNativeLibPackageName(TranslationUnit),
                    ClassName
                };

                var filePath = string.Join(Path.DirectorySeparatorChar.ToString(), names);
                return $"{filePath}.{FileExtension}";
            }
        }

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.JavaDoc);

            GenerateJavaPackage(TranslationUnit);
            GenerateJavaImports();

            TranslationUnit.Visit(this);
        }

        public override bool  VisitTranslationUnit(TranslationUnit unit)
        {
            WriteLine($"public interface {ClassName} extends com.sun.jna.Library");
            WriteStartBraceIndent();

            WriteLine($"{ClassName} INSTANCE = ({ClassName})");
            var libName = unit.FileNameWithoutExtension;
            WriteLineIndent($"Native.loadLibrary((com.sun.jna.Platform.isWindows() ? \"{libName}.dll\" : \"lib{libName}.dylib\"),");
            WriteLineIndent($"{ClassName}.class);");
            NewLine();

            var ret = base.VisitTranslationUnit(unit);

            WriteCloseBraceIndent();
            return ret;
        }

        public override bool VisitMethodDecl(Method method)
        {
            PushBlock(BlockKind.Method, method);

            var @class = method.Namespace as Class;
            Write($"public {method.ReturnType} {CCodeGenerator.GetMethodIdentifier(method)}(");
            Write(TypePrinter.VisitParameters(method.Parameters, hasNames: true).ToString());
            Write(");");

            PopBlock(NewLineKind.Never);
            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            VisitDeclContext(@class);
            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            return true;
        }

        public override bool VisitNamespace(Namespace @namespace)
        {
            return VisitDeclContext(@namespace);
        }
    }
}
