﻿using CppSharp;
using CppSharp.AST;
using System;
using System.Linq;

namespace MonoEmbeddinator4000.Generators
{
    public class CManagedToNativeTypePrinter : CppTypePrinter
    {
        Parameter param;

        public bool IsByRefParameter => param != null && (param.IsOut || param.IsInOut);

        public override string VisitDecayedType(DecayedType decayed,
            TypeQualifiers quals)
        {
            var managedArray = decayed as ManagedArrayType;
            if (managedArray != null)
                return managedArray.Typedef.Visit(this);

            return base.VisitDecayedType(decayed, quals);
        }

        public override string VisitCILType(CILType type, TypeQualifiers quals)
        {
            throw new System.NotImplementedException(
                string.Format("Unhandled .NET type: {0}", type.Type));
        }

        public override string VisitClassDecl(Class @class)
        {
            var type = base.VisitClassDecl(@class);
            return IsByRefParameter ? $"{type}*" : type;
        }

        public override string VisitParameter(Parameter arg, bool hasName = true)
        {
            Parameter prev = param;
            param = arg;
            var ret = base.VisitParameter(arg, hasName);
            param = prev;
            return ret;
        }

        public override string VisitPrimitiveType(PrimitiveType primitive)
        {
            switch (primitive)
            {
                case PrimitiveType.Char: return "gunichar2";
                case PrimitiveType.SChar: return "int8_t";
                case PrimitiveType.UChar: return "uint8_t";
                case PrimitiveType.Short: return "int16_t";
                case PrimitiveType.UShort: return "uint16_t";
                case PrimitiveType.Int: return "int32_t";
                case PrimitiveType.UInt: return "uint32_t";
                case PrimitiveType.Long: return "int64_t";
                case PrimitiveType.ULong: return "uint64_t";
                case PrimitiveType.String:
                {
                    if (param != null && (param.IsOut || param.IsInOut))
                        return "GString*";

                    return "const char*";
                }
                case PrimitiveType.Decimal: return "MonoDecimal";
            }

            return base.VisitPrimitiveType(primitive);
        }
    }

    public class CArrayTypePrinter : CppTypePrinter
    {
        static string AsCIdentifier(string id)
        {
            return new string(id.Where(c => char.IsLetterOrDigit(c) || c == '_')
                .ToArray());
        }

        public override string VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            var typeName = array.Type.Visit(this);

            if (string.IsNullOrWhiteSpace(typeName))
                throw new Exception($"Unhandled array type printing for type '{array.Type}'");

            typeName = AsCIdentifier(typeName);
            typeName = StringHelpers.Capitalize(typeName);

            return string.Format("{0}Array", typeName);
        }

        public override string VisitUnsupportedType(UnsupportedType type, TypeQualifiers quals)
        {
            return type.Description;
        }

        public override string VisitPrimitiveType(PrimitiveType primitive)
        {
            switch(primitive)
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