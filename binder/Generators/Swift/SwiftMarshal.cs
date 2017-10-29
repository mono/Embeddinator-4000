using CppSharp.AST;
using CppSharp.AST.Extensions;

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
            Context.Return.Write($"{Context.ArgName}");
            return true;
        }

        public void HandleRefOutPrimitiveType(PrimitiveType type, Enumeration @enum = null)
        {
            Context.Return.Write(Context.ArgName);
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            Context.Return.Write(Context.ArgName);
            return true;
        }

        public override bool VisitPrimitiveType(PrimitiveType type,
            TypeQualifiers quals)
        {
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
            Context.Return.Write(Context.ReturnVarName);
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
            Context.Return.Write(Context.ReturnVarName);
            return true;
        }
    }
}
