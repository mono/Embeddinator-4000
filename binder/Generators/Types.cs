using CppSharp.AST;

namespace Embeddinator.Generators
{
    public class ManagedArrayType : DecayedType
    {
        public ArrayType Array { get { return Decayed.Type as ArrayType; } }
        public TypedefType Typedef { get { return Original.Type as TypedefType; } }

        public ManagedArrayType(ArrayType array, TypedefType typedef)
        {
            Decayed = new QualifiedType(array);
            Original = new QualifiedType(typedef);
        }
    }

    public class CSharpTypePrinter : CppTypePrinter
    {
        public override string VisitUnsupportedType(UnsupportedType type, TypeQualifiers quals)
        {
            return type.Description;
        }

        public override string VisitPrimitiveType(PrimitiveType primitive)
        {
            switch (primitive)
            {
                case PrimitiveType.Bool:
                    return "Bool";
                case PrimitiveType.Char:
                    return "Char";
                case PrimitiveType.SChar:
                    return "SByte";
                case PrimitiveType.UChar:
                    return "Byte";
                case PrimitiveType.Short:
                    return "Int16";
                case PrimitiveType.UShort:
                    return "UInt16";
                case PrimitiveType.Int:
                    return "Int32";
                case PrimitiveType.UInt:
                    return "UInt32";
                case PrimitiveType.Long:
                    return "Int64";
                case PrimitiveType.ULong:
                    return "UInt64";
                case PrimitiveType.Float:
                    return "Single";
                case PrimitiveType.Double:
                    return "Double";
                case PrimitiveType.String:
                    return "String";
                case PrimitiveType.Decimal:
                    return "Decimal";
            }

            return base.VisitPrimitiveType(primitive);
        }
    }
}
