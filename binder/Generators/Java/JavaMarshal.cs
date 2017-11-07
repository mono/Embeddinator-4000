﻿﻿using CppSharp.AST;
using CppSharp.Generators;

namespace Embeddinator.Generators
{
    public class JavaMarshalPrinter : CMarshalPrinter
    {
        public JavaTypePrinter TypePrinter;

        public JavaMarshalPrinter(BindingContext context)
            : base(context)
        {
            TypePrinter = new JavaTypePrinter(Context);
        }
    }

    public class JavaMarshalManagedToNative : JavaMarshalPrinter
    {
        public JavaMarshalManagedToNative(BindingContext context)
            : base(context)
        {
        }

        public override bool VisitManagedArrayType(ManagedArrayType array,
            TypeQualifiers quals)
        {
            Return.Write("null");
            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            var @object = IsByRefParameter ? $"{ArgName}.get()" : ArgName;
            var objectRef = @class.IsInterface ? "__getObject()" : "__object";

            if (IsByRefParameter)
            {
                CheckRefOutParameter(Parameter.IsInOut);

                TypePrinter.PushContext(TypePrinterContextKind.Native);
                var typeName = Parameter.Visit(TypePrinter);
                TypePrinter.PopContext();

                var varName = JavaGenerator.GeneratedIdentifier(ArgName);
                var marshal = Parameter.IsInOut ?
                    $"new {typeName}({@object}.{objectRef})" : $"new {typeName}()";
                Before.WriteLine($"{typeName} {varName} = {marshal};");

                var marshaler = new JavaMarshalNativeToManaged(Context)
                {
                    Parameter = Parameter,
                    ReturnVarName = $"{varName}.getValue()"
                };

                @class.Visit(marshaler);

                After.WriteLine($"{ArgName}.set({marshaler.Return});");

                Return.Write(varName);
                return true;
            }

            Return.Write($"{ArgName} == null ? null : {ArgName}.{objectRef}");
            return true;
        }

        public void CheckRefOutParameter(bool nullCheck)
        {
            // Perform null checking for all primitive value types.
            string extraCheck = nullCheck ? $" || {ArgName}.get() == null" : string.Empty;
            Before.WriteLine($"if ({ArgName} == null{extraCheck})");
            Before.WriteLineIndent($"throw new NullRefParameterException(\"{Parameter.Name}\");");
        }

        static bool IsReferenceIntegerType(PrimitiveType type)
        {
            switch(type)
            {
            case PrimitiveType.UChar:
            case PrimitiveType.UShort:
            case PrimitiveType.UInt:
            case PrimitiveType.ULong:
            case PrimitiveType.ULongLong:
                return true;
            }
            return false;
        }

        static PrimitiveType GetSignedIntegerType(PrimitiveType type)
        {
            switch(type)
            {
            case PrimitiveType.UChar:
                return PrimitiveType.SChar;
            case PrimitiveType.UShort:
                return PrimitiveType.Short;
            case PrimitiveType.UInt:
                return PrimitiveType.Int;
            case PrimitiveType.ULong:
                return PrimitiveType.Long;
            case PrimitiveType.ULongLong:
                return PrimitiveType.LongLong;
            }
            throw new System.NotImplementedException();
        }

        public void HandleRefOutPrimitiveType(PrimitiveType type, Enumeration @enum = null)
        {
            TypePrinter.PushContext(TypePrinterContextKind.Native);
            var typeName = Parameter.Visit(TypePrinter);
            TypePrinter.PopContext();

            CheckRefOutParameter(type != PrimitiveType.String && Parameter.IsInOut);

            string marshal = $"{ArgName}.get()";

            var isEnum = @enum != null;
            if (isEnum)
                marshal = $"{marshal}.getValue()";

            if (type == PrimitiveType.Bool)
                marshal = $"((byte) ({marshal} ? 1 : 0))";

            if (IsReferenceIntegerType(type))
            {
                var integerTypeName = TypePrinter.VisitPrimitiveType(GetSignedIntegerType(type));
                marshal = $"({integerTypeName}){marshal}.intValue()";
            }

            var varName = JavaGenerator.GeneratedIdentifier(ArgName);

            Before.Write($"{typeName} {varName} = ");

            if (isEnum || type == PrimitiveType.Bool || IsReferenceIntegerType(type))
                Before.WriteLine($"new {typeName}({marshal});");
            else
                Before.WriteLine($"({marshal}) != null ? new {typeName}({marshal}) : new {typeName}();");

            Return.Write(varName);

            var value = $"{varName}.getValue()";
            marshal = value;

            if (isEnum)
                marshal = $"{@enum.Visit(TypePrinter)}.fromOrdinal({value})";

            if (type == PrimitiveType.Bool)
                marshal = $"{value} != 0";

            if (IsReferenceIntegerType(type))
            {
                var integerTypeName = Parameter.Type.Visit(TypePrinter);
                marshal = $"new {integerTypeName}({value})";
            }

            After.WriteLine($"{ArgName}.set({marshal});");
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (IsByRefParameter)
            {
                HandleRefOutPrimitiveType(@enum.BuiltinType.Type, @enum);
                return true;
            }

            var typeName = @enum.BuiltinType.Visit(TypePrinter);
            var varName = JavaGenerator.GeneratedIdentifier(ArgName);
            Before.WriteLine($"{typeName} {varName} = {ArgName}.getValue();");

            Return.Write(varName);
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
            else if (type == PrimitiveType.Decimal)
            {
                Return.Write($"new mono.embeddinator.Decimal({ArgName})");
                return true;
            }
            else if (type == PrimitiveType.Bool)
            {
                Return.Write($"(byte)({ArgName}? 1 : 0)");
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

    public class JavaMarshalNativeToManaged : JavaMarshalPrinter
    {
        public JavaMarshalNativeToManaged (BindingContext context)
            : base(context)
        {
        }

        public override bool VisitManagedArrayType(ManagedArrayType array,
            TypeQualifiers quals)
        {
            Return.Write("null");
            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            var typePrinter = new JavaTypePrinter(Context);
            var typeName = @class.Visit(typePrinter);

            if (@class.IsInterface || @class.IsAbstract)
                typeName = $"{typeName}Impl";

            Return.Write("({0} == com.sun.jna.Pointer.NULL ? null : new {1}({0}))",
                ReturnVarName, typeName);
            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            Return.Write($"{@enum.Visit(TypePrinter)}.fromOrdinal({ReturnVarName})");
            return true;
        }

        public override bool VisitPrimitiveType(PrimitiveType type,
            TypeQualifiers quals)
        {
            if(type == PrimitiveType.Bool)
                Return.Write($"{ReturnVarName} != 0");
            else if (type == PrimitiveType.Decimal)
                Return.Write($"{ReturnVarName}.getValue()");
            else
                Return.Write(ReturnVarName);
            return true;
        }
    }
}
