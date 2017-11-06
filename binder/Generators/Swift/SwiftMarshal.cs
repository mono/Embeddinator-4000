using CppSharp.AST;

namespace Embeddinator.Generators
{
    public class SwiftMarshalPrinter : CMarshalPrinter
    {
        public SwiftTypePrinter TypePrinter;

        public SwiftMarshalPrinter(MarshalContext marshalContext)
            : base(marshalContext)
        {
            TypePrinter = new SwiftTypePrinter(Context.Context);
        }
    }

    public class SwiftMarshalManagedToNative : SwiftMarshalPrinter
    {
        public SwiftMarshalManagedToNative(MarshalContext marshalContext)
            : base(marshalContext)
        {
        }

        public override bool VisitArrayType(ArrayType array,
            TypeQualifiers quals)
        {
            Context.Return.Write("Pointer.NULL");
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
            Context.Return.Write($"{Context.ArgName}.{objectRef}");
            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            Context.Return.Write(Context.ArgName);
            return true;
        }

        void HandleDecimalType()
        {
            var decimalId = SwiftGenerator.GeneratedIdentifier($"{Context.Parameter.Name}_decimal");
            var @var = IsByRefParameter ? "var" : "let";
            Context.SupportBefore.WriteLine($"{@var} {decimalId} : MonoDecimal = mono_embeddinator_string_to_decimal(\"\")");

            var pointerId = "pointer";
            if (IsByRefParameter)
            {
                Context.SupportBefore.WriteLine($"withUnsafeMutablePointer(to: &{decimalId}) {{ ({pointerId}) in");
                Context.SupportAfter.WriteLine("}");
            }

            Context.Return.Write(IsByRefParameter ? pointerId : decimalId);
        }

        public void HandleRefOutPrimitiveType(PrimitiveType type)
        {
            if (type == PrimitiveType.String)
            {
                var gstringId = $"{Context.ReturnVarName}_gstring";
                Context.SupportBefore.WriteLine($"let {gstringId} : UnsafeMutablePointer<GString> = g_string_new(\"\")");

                Context.SupportBefore.WriteLine($"g_string_free({gstringId}, 1)");

                Context.Return.Write(gstringId);
                return;
            }
            else if (type == PrimitiveType.Decimal)
            {
                HandleDecimalType();
                return;
            }

            Context.Return.Write($"&{Context.ArgName}");
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
                Context.Return.Write($"gunichar2({Context.ArgName}.unicodeScalars.first!.value)");
                return true;
            }
            else if (type == PrimitiveType.Decimal)
            {
                HandleDecimalType();
                return true;
            }

            Context.Return.Write(Context.ArgName);
            return true;
        }

        public override bool VisitParameterDecl(Parameter parameter)
        {
            var ret = base.VisitParameterDecl(parameter);
            return ret;
        }
    }

    public class SwiftMarshalNativeToManaged : SwiftMarshalPrinter
    {
        public SwiftMarshalNativeToManaged (MarshalContext marshalContext)
            : base(marshalContext)
        {
        }

        public override bool VisitManagedArrayType(ManagedArrayType array,
            TypeQualifiers quals)
        {
            var typeName = array.Visit(TypePrinter);
            Context.Return.Write($"new [{typeName}]");
            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            var typePrinter = new SwiftTypePrinter(Context.Context);
            var typeName = @class.Visit(typePrinter);

            //if (@class.IsInterface || @class.IsAbstract)
                //typeName = $"{typeName}Impl";

            Context.Return.Write($"{typeName}()");

            //Context.Return.Write(Context.ReturnVarName);
            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            Context.Return.Write(Context.ReturnVarName);
            return true;
        }

        public override bool VisitPrimitiveType(PrimitiveType type,
            TypeQualifiers quals)
        {
            if (type == PrimitiveType.Char)
            {
                Context.Return.Write($"Character(Unicode.Scalar({Context.ReturnVarName})!)");
                return true;
            }
            else if (type == PrimitiveType.String)
            {
                Context.Return.Write($"String(cString: {Context.ReturnVarName})");
                return true;
            }
            else if (type == PrimitiveType.Decimal)
            {
                HandleDecimalType();
                return true;
            }

            Context.Return.Write(Context.ReturnVarName);
            return true;
        }

        void HandleDecimalType()
        {
            var gstringId = $"{Context.ReturnVarName}_gstring";
            Context.SupportBefore.Write($"let {gstringId} : UnsafeMutablePointer<GString> = ");
            Context.SupportBefore.WriteLine($"mono_embeddinator_decimal_to_gstring({Context.ReturnVarName})");

            var stringId = $"{Context.ReturnVarName}_string";
            Context.SupportBefore.WriteLine($"let {stringId} : String = String(cString: {gstringId}.pointee.str)");

            var decimalId = $"{Context.ReturnVarName}_decimal";
            Context.SupportBefore.WriteLine($"let {decimalId} : Decimal = Decimal(string: {stringId})!");

            Context.SupportBefore.WriteLine($"g_string_free({gstringId}, 1)");

            Context.Return.Write(decimalId);
        }
    }
}
