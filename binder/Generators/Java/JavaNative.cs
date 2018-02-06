﻿using System.Collections.Generic;
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
    public class JavaNative : JavaSources
    {
        public JavaNative(BindingContext context, TranslationUnit unit)
            : base(context, unit)
        {
        }

        public static string GetNativeLibClassName(TranslationUnit unit) =>
            GetNativeLibClassName(unit.FileName);

        public static string GetNativeLibClassName(string fileName) =>
            $"Native_{JavaGenerator.FileNameAsIdentifier(fileName)}";

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
            GenerateFilePreamble(CommentKind.JavaDoc, "Embeddinator-4000");

            GenerateJavaPackage(TranslationUnit);
            GenerateJavaImports();

            TranslationUnit.Visit(this);
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            WriteLine($"public interface {ClassName} extends com.sun.jna.Library");
            WriteStartBraceIndent();

            WriteLine($"{ClassName} INSTANCE = ");
            var libName = unit.FileNameWithoutExtension;
            WriteLineIndent($"mono.embeddinator.Runtime.loadLibrary(\"{libName}\", {ClassName}.class);");
            NewLine();

            var ret = base.VisitTranslationUnit(unit);

            WriteCloseBraceIndent();
            return ret;
        }

        public static string GetCMethodIdentifier(Method method)
        {
            return CCodeGenerator.GeneratedMethodNames[method.ManagedQualifiedName()];
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
            Write($"public {returnTypeName} {GetCMethodIdentifier(method)}(");
            Write(TypePrinter.VisitParameters(method.Parameters, hasNames: true).ToString());
            Write(");");

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
            if (!VisitDeclaration(@enum))
                return false;

            return true;
        }
    }
}
