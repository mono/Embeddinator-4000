﻿using CppSharp;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;

namespace Embeddinator.Generators
{
    public class CMarshalPrinter : Marshaler
    {
        public CMarshalPrinter(BindingContext context)
            : base(context)
        {
        }

        public Options Options => Context.Options as Options;

        public bool IsByRefParameter => (Parameter != null) &&
            (Parameter.IsOut || Parameter.IsInOut);

        public CManagedToNativeTypePrinter CTypePrinter =>
            CGenerator.GetCTypePrinter(GeneratorKind.C);

        public virtual bool VisitManagedArrayType(ManagedArrayType array,
            TypeQualifiers quals)
        {
            return true;
        }

        public override bool VisitDecayedType(DecayedType decayed,
            TypeQualifiers quals)
        {
            var managedArray = decayed as ManagedArrayType;
            if (managedArray != null)
                return VisitManagedArrayType(managedArray, quals);

            return VisitDecayedType(decayed, quals);
        }
    }

    public class CMarshalManagedToNative : CMarshalPrinter
    {
        public bool UnboxPrimitiveValues { get; set; }

        public CMarshalManagedToNative(BindingContext context)
            : base(context)
        {
            UnboxPrimitiveValues = true;
        }

        public override bool VisitManagedArrayType(ManagedArrayType array,
            TypeQualifiers quals)
        {
            var support = Before;

            var arrayId = CGenerator.GenId($"{ArgName}_array");
            support.WriteLine("MonoArray* {0} = (MonoArray*) {1};",
                                            arrayId, ArgName);

            var arraySizeId = CGenerator.GenId($"{ArgName}_array_size");
            support.WriteLine("uintptr_t {0} = mono_array_length({1});",
                                            arraySizeId, arrayId);

            var typePrinter = CTypePrinter;
            typePrinter.PrintScopeKind = TypePrintScopeKind.Local;
            var arrayTypedefName = array.Typedef.Visit(typePrinter);

            typePrinter.PrintScopeKind = TypePrintScopeKind.Qualified;
            var arrayElementName = array.Array.Type.Visit(typePrinter);
            if (array.Array.Type.IsClass())
                arrayElementName += "*";
            var elementSize = $"sizeof({arrayElementName})";
    
            var nativeArrayId = CGenerator.GenId($"{ArgName}_native_array");
            support.WriteLine("{0} {1};", arrayTypedefName, nativeArrayId);
            support.WriteLine("{0}.array = g_array_sized_new(/*zero_terminated=*/FALSE," +
                " /*clear_=*/TRUE, {1}, {2});", nativeArrayId, elementSize, arraySizeId);

            var elementClassId = CGenerator.GenId($"{ArgName}_element_class");
            support.WriteLine("MonoClass* {0} = mono_class_get_element_class({1});",
                elementClassId,
                CMarshalNativeToManaged.GenerateArrayTypeLookup(array.Array.Type, support));

            var elementSizeId = CGenerator.GenId($"{ArgName}_array_element_size");
            support.WriteLine("gint32 {0} = mono_class_array_element_size({1});",
                elementSizeId, elementClassId);

            var iteratorId = CGenerator.GenId("i");
            support.WriteLine("for (int {0} = 0; {0} < {1}; {0}++)",
                iteratorId, arraySizeId);
            support.WriteStartBraceIndent();

            var elementId = CGenerator.GenId($"{ArgName}_array_element");
            
            if (CMarshalNativeToManaged.IsValueType(array.Array.Type))
            {
                var addressId = $"mono_array_addr_with_size({arrayId}, {elementSizeId}, {iteratorId})";
                if (array.Array.Type.IsClass())
                    support.WriteLine("MonoObject* {0} = mono_value_box({1}.domain, {2}, {3});",
                        elementId, CGenerator.GenId("mono_context"), elementClassId, addressId);
                else
                    support.WriteLine("char* {0} = {1};",
                        elementId, addressId);
            }
            else
                support.WriteLine("MonoObject* {0} = *(MonoObject**) mono_array_addr_with_size({1}, {2}, {3});",
                    elementId, arrayId, elementSizeId, iteratorId);

            var marshal = new CMarshalManagedToNative(Context)
            {
                ArgName = elementId,
                ReturnVarName = elementId,
                ReturnType = array.Array.QualifiedType,
                UnboxPrimitiveValues = false
            };

            array.Array.QualifiedType.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(marshal.Before))
                support.Write(marshal.Before.ToString());

            support.WriteLine("g_array_append_val({0}.array, {1});", nativeArrayId,
                marshal.Return.ToString());

            support.WriteCloseBraceIndent();

            Return.Write("{0}", nativeArrayId);
            return false;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            VisitPrimitiveType(@enum.BuiltinType.Type);
            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            var typeName = @class.Visit(CTypePrinter);
            var objectId = $"{ArgName}_obj";
            Before.WriteLine("{1}* {0} = {2} ? ({1}*) mono_embeddinator_create_object({2}) : 0;",
                objectId, typeName, ArgName);
            Return.Write("{0}", objectId);
            return true;
        }

        public override bool VisitCILType(CILType type, TypeQualifiers quals)
        {
            return base.VisitCILType(type, quals);
        }

        public override bool VisitBuiltinType(BuiltinType builtin,
            TypeQualifiers quals)
        {
            return VisitPrimitiveType(builtin.Type);
        }

        public override bool VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            var pointee = pointer.Pointee;

            PrimitiveType primitive;
            if (pointee.IsPrimitiveType(out primitive))
            {
                Return.Write("{0}", ArgName);
                return true;
            }

            return base.VisitPointerType(pointer, quals);
        }

        public bool VisitPrimitiveType(PrimitiveType primitive)
        {
            switch (primitive)
            {
                case PrimitiveType.Void:
                    return true;
                case PrimitiveType.Bool:
                case PrimitiveType.WideChar:
                case PrimitiveType.Char:
                case PrimitiveType.SChar:
                case PrimitiveType.UChar:
                case PrimitiveType.Short:
                case PrimitiveType.UShort:
                case PrimitiveType.Int:
                case PrimitiveType.UInt:
                case PrimitiveType.Long:
                case PrimitiveType.ULong:
                case PrimitiveType.LongLong:
                case PrimitiveType.ULongLong:
                case PrimitiveType.Float:
                case PrimitiveType.Double:
                case PrimitiveType.Decimal:
                case PrimitiveType.Null:
                {
                    var typePrinter = CTypePrinter;
                    var typeName = ReturnType.Visit(typePrinter);

                    var valueId = ArgName;

                    if (UnboxPrimitiveValues)
                    {
                        var unboxId = CGenerator.GenId("unbox");
                        Before.WriteLine("void* {0} = mono_object_unbox({1});",
                            unboxId, ArgName);
                        valueId = unboxId;
                    }

                    Return.Write("*(({0}*){1})", typeName, valueId);
                    return true;
                }
                case PrimitiveType.String:
                {
	                var stringId = CGenerator.GenId("string");
	                Before.WriteLine("char* {0} = mono_string_to_utf8(" +
	                    "(MonoString*) {1});", stringId, ArgName);
	
	                Return.Write("{0}", stringId);
	                return true;
                }
            }

            throw new System.NotImplementedException(primitive.ToString());
        }
    }

    public class CMarshalNativeToManaged : CMarshalPrinter
    {
        public bool PrimitiveValuesByValue { get; set; }

        public CMarshalNativeToManaged (BindingContext context)
            : base (context)
        {
            PrimitiveValuesByValue = false;
        }

        public static string GetMonoClassForPrimitiveType(PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.Void:
                    return "mono_get_void_class()";
                case PrimitiveType.Bool:
                    return "mono_get_boolean_class()";
                case PrimitiveType.Char:
                    return "mono_get_char_class()";
                case PrimitiveType.SChar:
                    return "mono_get_sbyte_class()";
                case PrimitiveType.UChar:
                    return "mono_get_byte_class()";
                case PrimitiveType.Short:
                    return "mono_get_int16_class()";
                case PrimitiveType.UShort:
                    return "mono_get_uint16_class()";
                case PrimitiveType.Int:
                case PrimitiveType.Long:
                    return "mono_get_int32_class()";
                case PrimitiveType.ULong:
                case PrimitiveType.UInt:
                    return "mono_get_uint32_class()";
                case PrimitiveType.LongLong:
                    return "mono_get_int64_class()";
                case PrimitiveType.ULongLong:
                    return "mono_get_uint64_class()";
                case PrimitiveType.Float:
                    return "mono_get_single_class()";
                case PrimitiveType.Double:
                    return "mono_get_double_class()";
                case PrimitiveType.IntPtr:
                    return "mono_get_intptr_class()";
                case PrimitiveType.UIntPtr:
                    return "mono_get_uintptr_class()";
                case PrimitiveType.String:
                    return "mono_get_string_class()";
                case PrimitiveType.Decimal:
                    return "mono_embeddinator_get_decimal_class()";
                default:
                    throw new System.NotImplementedException();
            }
        }

        public static string GenerateArrayTypeLookup(Type type, TextGenerator gen)
        {
            type = type.Desugar();

            if (type is BuiltinType)
            {
                var builtinType = type as BuiltinType;
                return GetMonoClassForPrimitiveType(builtinType.Type);
            }
            else if (type is CILType)
            {
                var cilType = type as CILType;

                if (cilType.Type == typeof(string))
                    return "mono_get_string_class()";

                return string.Format("mono_embeddinator_search_class(\"{0}\", \"{1}\", \"{2}\")",
                    cilType.Type.Assembly.GetName().Name, cilType.Type.Namespace,
                    cilType.Type.Name);
            }
            else if (type is TagType)
            {
                var tagType = type as TagType;
                var decl = tagType.Declaration;

                var classId = $"class_{decl.QualifiedName}";
                gen.WriteLine($"{classId} = {CSources.GenerateMonoClassFromNameCall(decl)}");

                return classId;
            }
            else if (type is ArrayType)
            {
                var arrayType = type as ArrayType;
                return "0";
            }

            throw new System.NotImplementedException();
        }

        public static bool IsValueType(Type type)
        {
            type = type.Desugar();

            if (type is BuiltinType && !type.IsPrimitiveType(PrimitiveType.String))
                return true;

            if (!(type is TagType))
                return false;

            Declaration decl;
            if (!type.TryGetDeclaration(out decl))
                return false;

            if (decl is Enumeration)
                return true;

            var @class = decl as Class;
            if (@class == null)
                return false;

            return @class.IsValueType;
        }

        public override bool VisitManagedArrayType(ManagedArrayType array,
            TypeQualifiers quals)
        {
            var support = Before;

            var contextId = CGenerator.GenId("mono_context");
            var arrayId = CGenerator.GenId($"{ArgName}_array");
            var elementClassId = CGenerator.GenId($"{ArgName}_element_class");

            var managedArray = array.Array;
            var elementType = managedArray.Type;
            support.WriteLine("MonoClass* {0} = mono_class_get_element_class({1});",
                elementClassId, GenerateArrayTypeLookup(elementType, support));

            support.WriteLine("MonoArray* {0} = mono_array_new({1}.domain, {2}, {3}.array->len);",
                arrayId, contextId, elementClassId, ArgName);

            var isValueType = IsValueType(elementType);

            var elementSizeId = string.Empty;
            if (array.Array.Type.IsClass() && isValueType)
            {
                elementSizeId = CGenerator.GenId($"{ArgName}_array_element_size");
                support.WriteLine("gint32 {0} = mono_class_array_element_size({1});",
                    elementSizeId, elementClassId);
            }

            var iteratorId = CGenerator.GenId("i");
            support.WriteLine("for (int {0} = 0; {0} < {1}.array->len; {0}++)",
                              iteratorId, ArgName);
            support.WriteStartBraceIndent();

            var typePrinter = CTypePrinter;
            string elementTypeName = elementType.Visit(typePrinter);

            var elementId = CGenerator.GenId($"{ArgName}_array_element");
            if (elementType.IsClass())
            {
                elementTypeName += "*";
                support.WriteLine("{0} {1} = g_array_index({2}.array, {0}, {3});",
                    elementTypeName, elementId, ArgName, iteratorId);
            }
            else
                support.WriteLine("{0} {1} = g_array_index({2}.array, {0}, {3});",
                    elementTypeName, elementId, ArgName, iteratorId);

            var marshal = new CMarshalNativeToManaged (Context)
            {
                ArgName = elementId,
                PrimitiveValuesByValue = true
            };

            elementType.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(marshal.Before))
                support.Write(marshal.Before.ToString());

            if (isValueType)
            {
                if (elementType.IsClass())
                {
                    var srcId = CGenerator.GenId("src");
                    var ptrId = CGenerator.GenId("ptr");
                    support.WriteLine("char* {0} = {1};", srcId, marshal.Return.ToString());
                    support.WriteLine("char* {0} = mono_array_addr_with_size({1}, {2}, {3});", 
                        ptrId, arrayId, elementSizeId, iteratorId);
                    support.WriteLine("memcpy({0}, {1}, {2});",
                        ptrId, srcId, elementSizeId);
                }
                else
                    support.WriteLine("mono_array_set({0}, {1}, {2}, {3});",
                        arrayId, elementTypeName, iteratorId,
                        marshal.Return.ToString());
            }
            else
            {
                support.WriteLine("mono_array_setref({0}, {1}, {2});",
                    arrayId, iteratorId, marshal.Return.ToString());
            }

            support.WriteCloseBraceIndent();

            Return.Write("{0}", arrayId);

            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            var byValue = PrimitiveValuesByValue ? string.Empty : "&";
            Return.Write($"{byValue}{ArgName}");
            return true;
        }

        public static string GenParamId(Marshaler marshal)
        {
            return $"{CGenerator.GenId(marshal.ArgName)}_{marshal.ParameterIndex}";
        }

        public override bool VisitClassDecl(Class @class)
        {
            var arg = IsByRefParameter ? $"(*{ArgName})" : ArgName;
            var handle = CSources.GetMonoObjectField(Options, CSources.MonoObjectFieldUsage.Parameter,
                arg, "_handle");

            var @object = $"{arg} ? mono_gchandle_get_target({handle}) : 0";

            if (@class.IsValueType)
                @object = $"mono_object_unbox({@object})";

            if (IsByRefParameter)
            {
                var argId = GenParamId(this);
                var objId = $"{argId}_obj";

                Before.WriteLine($"MonoObject* {objId} = {@object};");
                Before.WriteLine($"MonoObject* {argId} = {objId};");

                After.WriteLine($"if ({objId} != {argId})");
                After.WriteStartBraceIndent();
                After.WriteLine($"mono_embeddinator_destroy_object({arg});");
                After.WriteLine($"{arg} = ({argId} != 0) ? mono_embeddinator_create_object({argId}) : 0;");
                After.WriteCloseBraceIndent();

                Return.Write($"&{argId}");
                return true;
            }

            Return.Write($"{@object}");
            return true;
        }

        public override bool VisitCILType(CILType type, TypeQualifiers quals)
        {
            return base.VisitCILType(type, quals);
        }

        public override bool VisitBuiltinType(BuiltinType builtin,
            TypeQualifiers quals)
        {
            return VisitPrimitiveType(builtin.Type);
        }

        public void HandleRefOutNonDefaultIntegerEnum(Enumeration @enum)
        {
            // This deals with marshaling of managed enums with non-C default
            // backing types (ie. enum : short).

            // Unlike C++ or Objective-C, C enums always have the default integer
            // type size, so we need to create a local variable of the right type
            // for marshaling with the managed runtime and cast it back to the
            // correct type.

            var backingType = @enum.BuiltinType.Type;
            var typePrinter = new CppTypePrinter();
            var integerType = typePrinter.VisitPrimitiveType(backingType);
            var newArgName = CGenerator.GenId(ArgName);
            Before.WriteLine("{0} {1} = *(({0}*) {2});",
                integerType, newArgName, ArgName);
            Return.Write("&{0}", newArgName);
            After.WriteLine("*{0} = ({1}) {2};", ArgName,
                @enum.Visit(CTypePrinter), newArgName);
        }

        public override bool VisitPointerType(PointerType pointer,
            TypeQualifiers quals)
        {
            var pointee = pointer.Pointee;

            Enumeration @enum;
            if (pointee.TryGetEnum(out @enum))
            {
                var backingType = @enum.BuiltinType.Type;
                if (backingType != PrimitiveType.Int)
                {
                    HandleRefOutNonDefaultIntegerEnum(@enum);
                    return true;
                }

                Return.Write("{0}", ArgName);
                return true;
            }

            if (pointee is ArrayType)
            {
                // TODO: Handle out/ref array types.
                Return.Write("0");
                return true;
            }

            PrimitiveType primitive;
            if (pointee.IsPrimitiveType(out primitive))
            {
                Return.Write("{0}", ArgName);
                return true;
            }

            return base.VisitPointerType(pointer, quals);
        }

        public bool VisitPrimitiveType(PrimitiveType primitive)
        {
            var param = Parameter;
            switch (primitive)
            {
                case PrimitiveType.Void:
                    return true;
                case PrimitiveType.Bool:
                case PrimitiveType.Char:
                case PrimitiveType.WideChar:
                case PrimitiveType.SChar:
                case PrimitiveType.UChar:
                case PrimitiveType.Short:
                case PrimitiveType.UShort:
                case PrimitiveType.Int:
                case PrimitiveType.UInt:
                case PrimitiveType.Long:
                case PrimitiveType.ULong:
                case PrimitiveType.LongLong:
                case PrimitiveType.ULongLong:
                case PrimitiveType.Float:
                case PrimitiveType.Double:
                case PrimitiveType.Decimal:
                case PrimitiveType.Null:
                {
                    var prefix = "&";
                    if (IsByRefParameter || PrimitiveValuesByValue)
                        prefix = string.Empty;
                    Return.Write ("{0}{1}", prefix, ArgName);
                    return true;
                }
                case PrimitiveType.String:
                {
                    var argId = GenParamId(this);
                    var contextId = CGenerator.GenId("mono_context");
                    var @string = ArgName;

                    if (IsByRefParameter)
                    {
                        @string = $"{ArgName}->str";
                        After.WriteLine("mono_embeddinator_marshal_string_to_gstring({0}, {1});",
                            ArgName, argId);
                    }

                    Before.WriteLine("MonoString* {0} = ({2}) ? mono_string_new({1}.domain, {2}) : 0;",
                        argId, contextId, @string);
                    Return.Write("{0}{1}", IsByRefParameter ? "&" : string.Empty, argId);
                    return true;
                }
            }

            throw new System.NotImplementedException(primitive.ToString());
        }
    }
}
