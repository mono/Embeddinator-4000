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

        public void HandleRefOutPrimitiveType(PrimitiveType type, Enumeration @enum = null)
        {
            TypePrinter.PushContext(TypePrinterContextKind.Native);
            var typeName = Context.Parameter.Visit(TypePrinter);
            TypePrinter.PopContext();

            // Perform null checking for all primitive value types.
            string extraCheck = (type != PrimitiveType.String && Context.Parameter.IsInOut) ?
                $" || {Context.ArgName}.get() == null" : string.Empty;
            Context.SupportBefore.WriteLine($"if ({Context.ArgName} == null{extraCheck})");
            Context.SupportBefore.WriteLineIndent($"throw new NullRefParameterException(\"{Context.Parameter.Name}\");");

            string marshal = $"{Context.ArgName}.get()";

            var isEnum = @enum != null;
            if (isEnum)
                marshal = $"{marshal}.getValue()";

            if (type == PrimitiveType.Bool)
                marshal = $"((byte) ({marshal} ? 1 : 0))";

            var varName = JavaGenerator.GeneratedIdentifier(Context.ArgName);

            Context.SupportBefore.Write($"{typeName} {varName} = ");

            if (isEnum || type == PrimitiveType.Bool)
                Context.SupportBefore.WriteLine($"new {typeName}({marshal});");
            else
                Context.SupportBefore.WriteLine($"({marshal}) != null ? new {typeName}({marshal}) : new {typeName}();");

            Context.Return.Write(varName);

            var value = $"{varName}.getValue()";
            marshal = value;

            if (isEnum)
                marshal = $"{@enum.Name}.fromOrdinal({value})";

            if (type == PrimitiveType.Bool)
                marshal = $"{value} != 0";

            Context.SupportAfter.WriteLine($"{Context.ArgName}.set({marshal});");
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (IsByRefParameter)
            {
                HandleRefOutPrimitiveType(@enum.BuiltinType.Type, @enum);
                return true;
            }

            var typeName = @enum.BuiltinType.Visit(TypePrinter);
            var varName = JavaGenerator.GeneratedIdentifier(Context.ArgName);
            Context.SupportBefore.WriteLine($"{typeName} {varName} = {Context.ArgName}.getValue();");

            Context.Return.Write(varName);
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
            Context.Return.Write($"{@enum.Name}.fromOrdinal({Context.ReturnVarName})");
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
