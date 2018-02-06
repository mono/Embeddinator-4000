using CppSharp.AST;
using CppSharp.Generators;

namespace Embeddinator.Generators
{
    public class SwiftMarshaler : CMarshaler
    {
        public SwiftTypePrinter TypePrinter;

        public SwiftMarshaler(BindingContext context)
            : base(context)
        {
            TypePrinter = new SwiftTypePrinter(Context);
        }
    }

    public class SwiftMarshalManagedToNative : SwiftMarshaler
    {
        public SwiftMarshalManagedToNative(BindingContext context)
            : base(context)
        {
        }

        public override bool VisitArrayType(ArrayType array,
            TypeQualifiers quals)
        {
            Return.Write("Pointer.NULL");
            return true;
        }

        public override bool VisitManagedArrayType(ManagedArrayType array,
            TypeQualifiers quals)
        {
            return VisitArrayType(array.Array, quals);
        }

        public override bool VisitClassDecl(Class @class)
        {
            var objectRef = @class.IsInterface ? "__getObject()" : "__object!";
            Return.Write($"{ArgName}.{objectRef}");
            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            Return.Write(ArgName);
            return true;
        }

        void HandleDecimalType()
        {
            var decimalId = SwiftGenerator.GeneratedIdentifier($"{Parameter.Name}_decimal");
            var @var = IsByRefParameter ? "var" : "let";
            Before.WriteLine($"{@var} {decimalId} : MonoDecimal = mono_embeddinator_string_to_decimal(\"\")");

            var pointerId = "pointer";
            if (IsByRefParameter)
            {
                Before.WriteLine($"withUnsafeMutablePointer(to: &{decimalId}) {{ ({pointerId}) in");
                After.WriteLine("}");
            }

            Return.Write(IsByRefParameter ? pointerId : decimalId);
        }

        public void HandleRefOutPrimitiveType(PrimitiveType type)
        {
            if (type == PrimitiveType.String)
            {
                var gstringId = $"{ReturnVarName}_gstring";
                Before.WriteLine($"let {gstringId} : UnsafeMutablePointer<GString> = g_string_new(\"\")");

                Before.WriteLine($"g_string_free({gstringId}, 1)");

                Return.Write(gstringId);
                return;
            }
            else if (type == PrimitiveType.Decimal)
            {
                HandleDecimalType();
                return;
            }

            Return.Write($"&{ArgName}");
        }

        public override bool VisitPrimitiveType(PrimitiveType type,
            TypeQualifiers quals)
        {
            if (IsByRefParameter)
            {
                HandleRefOutPrimitiveType(type);
                return true;
            }

            if (type == PrimitiveType.Char)
            {
                Return.Write($"gunichar2({ArgName}.unicodeScalars.first!.value)");
                return true;
            }
            else if (type == PrimitiveType.Decimal)
            {
                HandleDecimalType();
                return true;
            }

            Return.Write(ArgName);
            return true;
        }

        public override bool VisitParameterDecl(Parameter parameter)
        {
            var ret = base.VisitParameterDecl(parameter);
            return ret;
        }
    }

    public class SwiftMarshalNativeToManaged : SwiftMarshaler
    {
        public SwiftMarshalNativeToManaged(BindingContext context)
            : base(context)
        {
        }

        public override bool VisitManagedArrayType(ManagedArrayType array,
            TypeQualifiers quals)
        {
            var typeName = array.Visit(TypePrinter);
            Return.Write($"new [{typeName}]");
            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            var typePrinter = new SwiftTypePrinter(Context);
            var typeName = @class.Visit(typePrinter);

            //if (@class.IsInterface || @class.IsAbstract)
                //typeName = $"{typeName}Impl";

            Return.Write($"{typeName}()");

            //Return.Write(ReturnVarName);
            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            Return.Write(ReturnVarName);
            return true;
        }

        public override bool VisitPrimitiveType(PrimitiveType type,
            TypeQualifiers quals)
        {
            if (type == PrimitiveType.Char)
            {
                Return.Write($"Character(Unicode.Scalar({ReturnVarName})!)");
                return true;
            }
            else if (type == PrimitiveType.String)
            {
                Return.Write($"String(cString: {ReturnVarName})");
                return true;
            }
            else if (type == PrimitiveType.Decimal)
            {
                HandleDecimalType();
                return true;
            }

            Return.Write(ReturnVarName);
            return true;
        }

        void HandleDecimalType()
        {
            var gstringId = $"{ReturnVarName}_gstring";
            Before.Write($"let {gstringId} : UnsafeMutablePointer<GString> = ");
            Before.WriteLine($"mono_embeddinator_decimal_to_gstring({ReturnVarName})");

            var stringId = $"{ReturnVarName}_string";
            Before.WriteLine($"let {stringId} : String = String(cString: {gstringId}.pointee.str)");

            var decimalId = $"{ReturnVarName}_decimal";
            Before.WriteLine($"let {decimalId} : Decimal = Decimal(string: {stringId})!");

            Before.WriteLine($"g_string_free({gstringId}, 1)");

            Return.Write(decimalId);
        }
    }
}
