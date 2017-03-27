using CppSharp;
using CppSharp.AST;
using System.Linq;

namespace MonoEmbeddinator4000.Generators
{
    public class CManagedToNativeTypePrinter : CppTypePrinter
    {
        Parameter param;

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
            if (type.Type == typeof(string))
            {
                if (param != null && (param.IsOut || param.IsInOut))
                    return "GString*";

                return quals.IsConst ? "const char*" : "char*";
            }

            throw new System.NotImplementedException(
                string.Format("Unhandled .NET type: {0}", type.Type));
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
            if (primitive == PrimitiveType.WideChar)
                return "gunichar2";

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
            typeName = AsCIdentifier(typeName);
            typeName = StringHelpers.Capitalize(typeName);

            return string.Format("{0}Array", typeName);
        }
    }
}