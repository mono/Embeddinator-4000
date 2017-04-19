using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.CSharp;
using CppSharp.Parser;
using System;
using System.Collections.Generic;
using CppSharp.AST.Extensions;

namespace MonoEmbeddinator4000.Generators
{
    public class JavaTypePrinterContext : CSharpTypePrinterContext
    {

    }

    public class JavaTypePrinter : CSharpTypePrinter
    {
        public JavaTypePrinter(BindingContext context) : base(context)
        {
        }

        public override TypePrinterResult VisitArrayType(ArrayType array,
            TypeQualifiers quals)
        {
            return string.Format("{0}[]", array.Type.Visit(this));
        }

        static string GetName(Declaration decl)
        {
            var names = new List<string>();

            names.AddRange(JavaSources.GetPackageNames(decl));
            names.Add(decl.Name);

            return string.Join(".", names);
        }

        public override TypePrinterResult VisitDeclaration(Declaration decl)
        {
            return GetName(decl);
        }

        public override TypePrinterResult VisitPointerType(PointerType pointer,
            TypeQualifiers quals)
        {
            var pointee = pointer.Pointee;

            Class @class;
            if (pointee.TryGetClass(out @class) && ContextKind == TypePrinterContextKind.Native)
                return JavaGenerator.IntPtrType;

            return pointer.QualifiedPointee.Visit(this);
        }

        public override TypePrinterResult VisitPrimitiveType(PrimitiveType primitive,
            TypeQualifiers quals)
        {
            // This uses JNA conventions, https://jna.java.net/javadoc/overview-summary.html#marshalling.
            switch (primitive)
            {
                case PrimitiveType.Bool:return "boolean";
                case PrimitiveType.Void: return "void";
                case PrimitiveType.Char16:
                case PrimitiveType.Char32:
                case PrimitiveType.WideChar: return "com.sun.jna.WString";
                case PrimitiveType.Char: return "char";
                case PrimitiveType.SChar: return "byte";
                case PrimitiveType.UChar: return "UnsignedByte";
                case PrimitiveType.Short: return "short";
                case PrimitiveType.UShort: return "UnsignedShort";
                case PrimitiveType.Int: return "int";
                case PrimitiveType.UInt: return "UnsignedInt";
                case PrimitiveType.Long: return "long";
                case PrimitiveType.ULong: return "UnsignedLong";
                case PrimitiveType.LongLong: return "LongLong";
                case PrimitiveType.ULongLong: return "UnsignedLongLong";
                case PrimitiveType.Int128: return "__int128";
                case PrimitiveType.UInt128: return "__uint128_t";
                case PrimitiveType.Half: return "__fp16";
                case PrimitiveType.Float: return "float";
                case PrimitiveType.Double: return "double";
                case PrimitiveType.LongDouble: return "decimal";
                case PrimitiveType.IntPtr:
                case PrimitiveType.UIntPtr:
                case PrimitiveType.Null: return "Pointer";
                case PrimitiveType.String: return "String";
            }

            throw new NotSupportedException();
        }
    }
}