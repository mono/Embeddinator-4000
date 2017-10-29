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
            switch (primitive)
            {
                case PrimitiveType.Bool:return "Bool";
                case PrimitiveType.Void: return "Void";
                case PrimitiveType.Char16:
                case PrimitiveType.Char32:
                case PrimitiveType.WideChar:
                case PrimitiveType.Char: return "Character";
                case PrimitiveType.SChar: return "Int8";
                case PrimitiveType.UChar: return "UInt8";
                case PrimitiveType.Short: return "Int16";
                case PrimitiveType.UShort: return "UInt16";
                case PrimitiveType.Int: return "Int32";
                case PrimitiveType.UInt: return "UInt32";
                case PrimitiveType.Long: return "Int64";
                case PrimitiveType.ULong: return "UInt64";
                case PrimitiveType.LongLong: return "LongLong";
                case PrimitiveType.ULongLong: return "UnsignedLongLong";
                case PrimitiveType.Int128: return "__int128";
                case PrimitiveType.UInt128: return "__uint128_t";
                case PrimitiveType.Half: return "__fp16";
                case PrimitiveType.Float: return "Float";
                case PrimitiveType.Double: return "Double";
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