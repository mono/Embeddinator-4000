using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace MonoEmbeddinator4000.Generators
{
    public class JavaMarshalPrinter : CMarshalPrinter
    {
        public JavaTypePrinter TypePrinter;

        public bool IsByRefParameter => (Context.Parameter != null) &&
            (Context.Parameter.IsOut || Context.Parameter.IsInOut);

        public JavaMarshalPrinter(MarshalContext marshalContext)
            : base(marshalContext)
        {
            TypePrinter = new JavaTypePrinter(Context.Context);
        }
    }

    public class JavaMarshalManagedToNative : JavaMarshalPrinter
    {
        public JavaMarshalManagedToNative(MarshalContext marshalContext)
            : base(marshalContext)
        {
        }

        public override bool VisitManagedArrayType(ManagedArrayType array,
            TypeQualifiers quals)
        {
            Context.Return.Write("null");
            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            Context.Return.Write($"{Context.ArgName}.__object");
            return true;
        }

        public void HandleRefOutPrimitiveType(PrimitiveType type, bool isEnum = false)
        {
            TypePrinter.PushContext(TypePrinterContextKind.Native);
            var typeName = Context.Parameter.Visit(TypePrinter);
            TypePrinter.PopContext();

            // Perform null checking for all primitive value types.
            if (type != PrimitiveType.String)
            {
                Context.SupportBefore.WriteLine($"if ({Context.ArgName}.get() == null)");
                Context.SupportBefore.WriteLineIndent($"throw new NullRefParameterException(\"{Context.Parameter.Name}\");");
            }

            string marshal = $"{Context.ArgName}.get()";

            // Special cases for enumerations and booleans.
            if (isEnum)
                marshal = $"{marshal}.getValue()";

            if (type == PrimitiveType.Bool)
                marshal = $"((byte) ({marshal} ? 1 : 0))";

            var varName = JavaGenerator.GeneratedIdentifier(Context.ArgName);
            Context.SupportBefore.WriteLine($"{typeName} {varName} = new {typeName}({marshal});");
            Context.Return.Write(varName);
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (IsByRefParameter)
            {
                HandleRefOutPrimitiveType(@enum.BuiltinType.Type, isEnum: true);
                return true;
            }

            Context.Return.Write(Context.ArgName);
            return true;
        }

        public override bool VisitPrimitiveType(PrimitiveType type,
            TypeQualifiers quals)
        {
            if (IsByRefParameter)
            {
                HandleRefOutPrimitiveType(type);
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

    public class JavaMarshalNativeToManaged : JavaMarshalPrinter
    {
        public JavaMarshalNativeToManaged (MarshalContext marshalContext)
            : base(marshalContext)
        {
        }

        public override bool VisitManagedArrayType(ManagedArrayType array,
            TypeQualifiers quals)
        {
            Context.Return.Write("null");
            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            var typePrinter = new JavaTypePrinter(Context.Context);
            var typeName = @class.Visit(typePrinter);

            Context.Return.Write($"new {typeName}({Context.ReturnVarName})");
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
