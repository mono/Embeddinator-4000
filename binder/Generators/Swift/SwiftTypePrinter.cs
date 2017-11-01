using CppSharp.AST;
using CppSharp.Generators;
using System;

namespace Embeddinator.Generators
{
    public class SwiftTypePrinter : TypePrinter
    {
        BindingContext Context { get; }

        bool IsByRefParameter => (Parameter != null) && (Parameter.IsOut || Parameter.IsInOut);

        public SwiftTypePrinter(BindingContext context)
        {
            Context = context;
        }

        public override TypePrinterResult VisitArrayType(ArrayType array,
            TypeQualifiers quals)
        {
            return $"[{array.Type.Visit(this)}]";
        }

        public override TypePrinterResult VisitDeclaration(Declaration decl)
        {
            return decl.QualifiedName;
        }

        public override TypePrinterResult VisitClassDecl(Class @class)
        {
            if (ContextKind == TypePrinterContextKind.Native)
                return VisitPrimitiveType(PrimitiveType.IntPtr);

            return VisitDeclaration(@class);
        }

        public override TypePrinterResult VisitParameter(Parameter param, bool hasName)
        {
            Parameter = param;
            var type = param.QualifiedType.Visit(this);
            var name = hasName ? $"{param.Name}" : string.Empty;
            Parameter = null;

            var inout = IsByRefParameter ? "inout " : string.Empty;
            return $"{name} : {inout}{type}";
        }

        public override TypePrinterResult VisitPointerType(PointerType pointer,
            TypeQualifiers quals)
        {
            var pointee = pointer.Pointee;
            return pointer.QualifiedPointee.Visit(this);
        }

        public override TypePrinterResult VisitPrimitiveType(PrimitiveType primitive,
            TypeQualifiers quals = new TypeQualifiers())
        {
            var isNative = ContextKind == TypePrinterContextKind.Native;

            switch (primitive)
            {
                case PrimitiveType.Bool: return isNative ? "CBool" : "Bool";
                case PrimitiveType.Void: return "Void";
                case PrimitiveType.Char16: return "CChar16";
                case PrimitiveType.Char32: return "CChar32";
                case PrimitiveType.WideChar: return "CWideChar";
                case PrimitiveType.Char: return "Character";
                case PrimitiveType.SChar: return isNative ? "CChar" : "Int8";
                case PrimitiveType.UChar: return isNative ? "CUnsignedChar" : "UInt8";
                case PrimitiveType.Short: return isNative ? "CShort" : "Int16";
                case PrimitiveType.UShort: return isNative ? "CUnsignedShort" : "UInt16";
                case PrimitiveType.Int: return isNative ? "CInt" : "Int32";
                case PrimitiveType.UInt: return isNative ? "CUnsignedInt" : "UInt32";
                case PrimitiveType.Long: return isNative ? "CLong" : "Int64";
                case PrimitiveType.ULong: return isNative ? "CUnsignedLong" : "UInt64";
                case PrimitiveType.LongLong: return isNative ? "CLongLong" : "LongLong";
                case PrimitiveType.ULongLong: return isNative ? "CUnsignedLongLong" : "UnsignedLongLong";
                case PrimitiveType.Int128: return "__int128";
                case PrimitiveType.UInt128: return "__uint128_t";
                case PrimitiveType.Half: return "__fp16";
                case PrimitiveType.Float: return isNative ? "CFloat" : "Float";
                case PrimitiveType.Double: return isNative ? "CDouble" : "Double";
                case PrimitiveType.IntPtr:
                case PrimitiveType.UIntPtr:
                case PrimitiveType.Null: return SwiftGenerator.IntPtrType;
                case PrimitiveType.String: return "String";
                case PrimitiveType.Decimal: return "Decimal";
            }

            throw new NotSupportedException();
        }

        public override TypePrinterResult VisitUnsupportedType(UnsupportedType type,
            TypeQualifiers quals)
        {
            return type.Description;
        }
    }
}