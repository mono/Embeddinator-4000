using CppSharp.AST;
using CppSharp.Generators;
using System;
using System.Collections.Generic;

namespace MonoEmbeddinator4000.Generators
{
    public class JavaTypePrinter : TypePrinter
    {
        BindingContext Context { get; }

        bool IsByRefParameter => (Parameter != null) && (Parameter.IsOut || Parameter.IsInOut);

        public JavaTypePrinter(BindingContext context)
        {
            Context = context;
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

        public override TypePrinterResult VisitParameter(Parameter param, bool hasName)
        {
            if ((param.IsInOut || param.IsOut) && ContextKind != TypePrinterContextKind.Native)
            {
                PushContext(TypePrinterContextKind.Template);
                var paramType = base.VisitParameter(param, false);
                PushContext(TypePrinterContextKind.Template);

                var usage = param.IsInOut ? "Ref" : "Out";
                return $"mono.embeddinator.{usage}<{paramType}> {param.Name}";
            }

            return base.VisitParameter(param, hasName);
        }

        public TypePrinterResult HandleNativeRefOutPrimitiveType(PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.Bool:
                case PrimitiveType.Char:
                case PrimitiveType.SChar:
                case PrimitiveType.UChar:
                    return "com.sun.jna.ptr.ByteByReference";
                case PrimitiveType.Short:
                case PrimitiveType.UShort:
                    return "com.sun.jna.ptr.ShortByReference";
                case PrimitiveType.Int:
                case PrimitiveType.UInt:
                    return "com.sun.jna.ptr.IntByReference";
                case PrimitiveType.Long:
                case PrimitiveType.ULong:
                    return "com.sun.jna.ptr.LongByReference";
                case PrimitiveType.Float:
                    return "com.sun.jna.ptr.FloatByReference";
                case PrimitiveType.Double:
                    return "com.sun.jna.ptr.DoubleByReference";
                case PrimitiveType.IntPtr:
                case PrimitiveType.UIntPtr:
                    return "com.sun.jna.ptr.PointerByReference";
                case PrimitiveType.String:
                    return "mono.embeddinator.GString";
                default:
                    return JavaGenerator.IntPtrType;
            }
        }

        public override TypePrinterResult VisitEnumDecl(Enumeration @enum)
        {
            if (ContextKind == TypePrinterContextKind.Native && IsByRefParameter)
                return HandleNativeRefOutPrimitiveType(@enum.BuiltinType.Type);

            return base.VisitEnumDecl(@enum);
        }

        public override TypePrinterResult VisitClassDecl(Class @class)
        {
            if (ContextKind == TypePrinterContextKind.Native)
                return JavaGenerator.IntPtrType;

            return VisitDeclaration(@class);
        }

        public override TypePrinterResult VisitPointerType(PointerType pointer,
            TypeQualifiers quals)
        {
            var pointee = pointer.Pointee;
            return pointer.QualifiedPointee.Visit(this);
        }

        public override TypePrinterResult VisitPrimitiveType(PrimitiveType primitive,
            TypeQualifiers quals)
        {
            if (ContextKind == TypePrinterContextKind.Native && IsByRefParameter)
                return HandleNativeRefOutPrimitiveType(primitive);

            bool useReferencePrimitiveTypes = ContextKind == TypePrinterContextKind.Template;

            // This uses JNA conventions, https://jna.java.net/javadoc/overview-summary.html#marshalling.
            switch (primitive)
            {
                case PrimitiveType.Bool:return useReferencePrimitiveTypes ? "Boolean" : "boolean";
                case PrimitiveType.Void: return "void";
                case PrimitiveType.Char16:
                case PrimitiveType.Char32:
                case PrimitiveType.WideChar: return "com.sun.jna.WString";
                case PrimitiveType.Char: return "char";
                case PrimitiveType.SChar: return useReferencePrimitiveTypes ? "Byte" : "byte";
                case PrimitiveType.UChar: return "UnsignedByte";
                case PrimitiveType.Short: return useReferencePrimitiveTypes ? "Short" : "short";
                case PrimitiveType.UShort: return "UnsignedShort";
                case PrimitiveType.Int: return useReferencePrimitiveTypes ? "Integer" : "int";
                case PrimitiveType.UInt: return "UnsignedInt";
                case PrimitiveType.Long: return useReferencePrimitiveTypes ? "Long" : "long";
                case PrimitiveType.ULong: return "UnsignedLong";
                case PrimitiveType.LongLong: return "LongLong";
                case PrimitiveType.ULongLong: return "UnsignedLongLong";
                case PrimitiveType.Int128: return "__int128";
                case PrimitiveType.UInt128: return "__uint128_t";
                case PrimitiveType.Half: return "__fp16";
                case PrimitiveType.Float: return useReferencePrimitiveTypes ? "Float" : "float";
                case PrimitiveType.Double: return useReferencePrimitiveTypes ? "Double" : "double";
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