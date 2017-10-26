﻿﻿using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace Embeddinator.Generators
{
    public class JavaMarshalPrinter : CMarshalPrinter
    {
        public JavaTypePrinter TypePrinter;

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
            Context.Return.Write("Pointer.NULL");
            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            var @object = IsByRefParameter ? $"{Context.ArgName}.get()" : Context.ArgName;
            var objectRef = @class.IsInterface ? "__getObject()" : "__object";

            if (IsByRefParameter)
            {
                CheckRefOutParameter(Context.Parameter.IsInOut);

                TypePrinter.PushContext(TypePrinterContextKind.Native);
                var typeName = Context.Parameter.Visit(TypePrinter);
                TypePrinter.PopContext();

                var varName = JavaGenerator.GeneratedIdentifier(Context.ArgName);
                var marshal = Context.Parameter.IsInOut ?
                    $"new {typeName}({@object}.{objectRef})" : $"new {typeName}()";
                Context.SupportBefore.WriteLine($"{typeName} {varName} = {marshal};");

                var marshalContext = new MarshalContext(Context.Context)
                {
                    Parameter = Context.Parameter,
                    ReturnVarName = $"{varName}.getValue()"
                };
                
                var marshaler = new JavaMarshalNativeToManaged(marshalContext);
                @class.Visit(marshaler);

                Context.SupportAfter.WriteLine($"{Context.ArgName}.set({marshaler.Context.Return});");

                Context.Return.Write(varName);
                return true;
            }

            Context.Return.Write($"{Context.ArgName} == null ? null : {Context.ArgName}.{objectRef}");
            return true;
        }

        public void CheckRefOutParameter(bool nullCheck)
        {
            // Perform null checking for all primitive value types.
            string extraCheck = nullCheck ? $" || {Context.ArgName}.get() == null" : string.Empty;
            Context.SupportBefore.WriteLine($"if ({Context.ArgName} == null{extraCheck})");
            Context.SupportBefore.WriteLineIndent($"throw new NullRefParameterException(\"{Context.Parameter.Name}\");");
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
            var typeName = Context.Parameter.Visit(TypePrinter);
            TypePrinter.PopContext();

            CheckRefOutParameter(type != PrimitiveType.String && Context.Parameter.IsInOut);

            string marshal = $"{Context.ArgName}.get()";

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

            var varName = JavaGenerator.GeneratedIdentifier(Context.ArgName);

            Context.SupportBefore.Write($"{typeName} {varName} = ");

            if (isEnum || type == PrimitiveType.Bool || IsReferenceIntegerType(type))
                Context.SupportBefore.WriteLine($"new {typeName}({marshal});");
            else
                Context.SupportBefore.WriteLine($"({marshal}) != null ? new {typeName}({marshal}) : new {typeName}();");

            Context.Return.Write(varName);

            var value = $"{varName}.getValue()";
            marshal = value;

            if (isEnum)
                marshal = $"{@enum.Visit(TypePrinter)}.fromOrdinal({value})";

            if (type == PrimitiveType.Bool)
                marshal = $"{value} != 0";

            if (IsReferenceIntegerType(type))
            {
                var integerTypeName = Context.Parameter.Type.Visit(TypePrinter);
                marshal = $"new {integerTypeName}({value})";
            }

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
            else if (type == PrimitiveType.Decimal)
            {
                Context.Return.Write($"new mono.embeddinator.Decimal({Context.ArgName})");
                return true;
            }
            else if (type == PrimitiveType.Bool)
            {
                Context.Return.Write($"(byte)({Context.ArgName}? 1 : 0)");
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
            var arraySizeId = JavaGenerator.GeneratedIdentifier("arrraySize");
            var arrayElementSizeId = JavaGenerator.GeneratedIdentifier("arrrayElementSize");

            Context.SupportBefore.WriteLine($"int {arrayElementSizeId} = {Context.ReturnVarName}.array.element_size;");
            Context.SupportBefore.WriteLine($"int {arraySizeId} = {Context.ReturnVarName}.array.len / {arrayElementSizeId};");

            var arrayId = JavaGenerator.GeneratedIdentifier("array");
            var arrayElementType = array.Array.Type.Visit(TypePrinter);
            Context.SupportBefore.WriteLine($"{arrayElementType}[] {arrayId} = new {arrayElementType}[{arraySizeId}];");

            // Marshal the array elements to the Java array.
            Context.SupportBefore.WriteLine($"for (int i = 0; i < {arrayElementSizeId}; ++i)");
            Context.SupportBefore.WriteStartBraceIndent();

            TypePrinter.PushContext(TypePrinterContextKind.Native);
            var arrayElementNativeType = array.Array.Type.Visit(TypePrinter);
            TypePrinter.PopContext();

            var arrayElementId = JavaGenerator.GeneratedIdentifier("arrayElement");

            Context.SupportBefore.Write($"{JavaGenerator.IntPtrType} {arrayElementId} = ");
            Context.SupportBefore.WriteLine($"{Context.ReturnVarName}.data.share(i * {arrayElementSizeId}, {arrayElementSizeId});");

            var elementType = array.Array.Type.Desugar();
            var marshalElementId = JavaGenerator.GeneratedIdentifier("marshalElement");

            bool didMarshal = true;

            PrimitiveType primitiveType;
            if (elementType.IsPrimitiveType(out primitiveType))
            {
                switch(primitiveType)
                {
                    case PrimitiveType.Bool:
                    case PrimitiveType.UChar:
                    case PrimitiveType.SChar:
                        Context.SupportBefore.WriteLine($"int {marshalElementId} = {arrayElementId}.getByte();");
                        break;
                    case PrimitiveType.Short:
                        Context.SupportBefore.WriteLine($"int {marshalElementId} = {arrayElementId}.getShort();");
                        break;
                    case PrimitiveType.Int:
                        Context.SupportBefore.WriteLine($"int {marshalElementId} = {arrayElementId}.getInt();");
                        break;
                    case PrimitiveType.Long:
                        Context.SupportBefore.WriteLine($"int {marshalElementId} = {arrayElementId}.getLong();");
                        break;
                    default:
                        didMarshal = false;
                        break;
                }
            }

            if (didMarshal)
                Context.SupportBefore.WriteLine($"{arrayId}.add({marshalElementId});");

            Context.SupportBefore.WriteCloseBraceIndent();

            Context.Return.Write(arrayId);
            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            var typePrinter = new JavaTypePrinter(Context.Context);
            var typeName = @class.Visit(typePrinter);

            if (@class.IsInterface || @class.IsAbstract)
                typeName = $"{typeName}Impl";

            Context.Return.Write("({0} == com.sun.jna.Pointer.NULL ? null : new {1}({0}))",
                Context.ReturnVarName, typeName);
            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            Context.Return.Write($"{@enum.Visit(TypePrinter)}.fromOrdinal({Context.ReturnVarName})");
            return true;
        }

        public override bool VisitPrimitiveType(PrimitiveType type,
            TypeQualifiers quals)
        {
            if(type == PrimitiveType.Bool)
                Context.Return.Write($"{Context.ReturnVarName} != 0");
            else if (type == PrimitiveType.Decimal)
                Context.Return.Write($"{Context.ReturnVarName}.getValue()");
            else
                Context.Return.Write(Context.ReturnVarName);
            return true;
        }
    }
}
