using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.CSharp;
using CppSharp.Parser;
using System;
using System.Collections.Generic;

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

        static string GetIntegerTypeName(PrimitiveType primitive,
            ParserTargetInfo targetInfo)
        {
            uint width;
            bool signed;

            GetPrimitiveTypeWidth(primitive, targetInfo, out width, out signed);

            switch (width)
            {
                case 8:
                    return signed ? "byte" : "UnsignedByte";
                case 16:
                    return signed ? "short" : "UnsignedShort";
                case 32:
                    return signed ? "int" : "UnsignedInt";
                case 64:
                    return signed ? "long" : "UnsignedLong";
                default:
                    throw new NotImplementedException();
            }
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
                case PrimitiveType.WideChar: return "WString";
                case PrimitiveType.Char: return "String";
                case PrimitiveType.SChar:
                case PrimitiveType.UChar:
                case PrimitiveType.Short:
                case PrimitiveType.UShort:
                case PrimitiveType.Int:
                case PrimitiveType.UInt:
                case PrimitiveType.Long:
                case PrimitiveType.ULong:
                case PrimitiveType.LongLong:
                case PrimitiveType.ULongLong:
                    return GetIntegerTypeName(primitive, Context.TargetInfo);
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