using CppSharp;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using System.Linq;

namespace MonoEmbeddinator4000.Generators
{
    public class CMarshalPrinter : MarshalPrinter<MarshalContext>
    {
        public CMarshalPrinter(MarshalContext marshalContext)
            : base(marshalContext)
        {
        }

        public CManagedToNativeTypePrinter CTypePrinter =>
            CGenerator.GetCTypePrinter(CppSharp.Generators.GeneratorKind.C);

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
        public Options Options { get; private set; }
        public bool UnboxPrimitiveValues { get; set; }

        public CMarshalManagedToNative(Options options, MarshalContext marshalContext)
            : base(marshalContext)
        {
            Options = options;
            UnboxPrimitiveValues = true;
        }

        public override bool VisitManagedArrayType(ManagedArrayType array,
            TypeQualifiers quals)
        {
            var support = Context.SupportBefore;

            var arrayId = CGenerator.GenId($"{Context.ArgName}_array");
            support.WriteLine("MonoArray* {0} = (MonoArray*) {1};",
                                            arrayId, Context.ArgName);

            var arraySizeId = CGenerator.GenId($"{Context.ArgName}_array_size");
            support.WriteLine("uintptr_t {0} = mono_array_length({1});",
                                            arraySizeId, arrayId);

            var typePrinter = CTypePrinter;
            typePrinter.PrintScopeKind = TypePrintScopeKind.Local;
            var arrayTypedefName = array.Typedef.Visit(typePrinter);

            typePrinter.PrintScopeKind = TypePrintScopeKind.Qualified;
            var arrayElementName = array.Array.Type.Visit(typePrinter);
            var elementSize = $"sizeof({arrayElementName})";
    
            var nativeArrayId = CGenerator.GenId($"{Context.ArgName}_native_array");
            support.WriteLine("{0} {1};", arrayTypedefName, nativeArrayId);
            support.WriteLine("{0}.array = g_array_sized_new(/*zero_terminated=*/FALSE," +
                " /*clear_=*/TRUE, {1}, {2});", nativeArrayId, elementSize, arraySizeId);

            var elementClassId = CGenerator.GenId($"{Context.ArgName}_element_class");
            support.WriteLine("MonoClass* {0} = mono_class_get_element_class({1});",
                elementClassId,
                CMarshalNativeToManaged.GenerateArrayTypeLookup(array.Array.Type, support));

            var elementSizeId = CGenerator.GenId($"{Context.ArgName}_array_element_size");
            support.WriteLine("gint32 {0} = mono_class_array_element_size({1});",
                elementSizeId, elementClassId);

            var iteratorId = CGenerator.GenId("i");
            support.WriteLine("for (int {0} = 0; {0} < {1}; {0}++)",
                iteratorId, arraySizeId);
            support.WriteStartBraceIndent();

            var elementId = CGenerator.GenId($"{Context.ArgName}_array_element");

            var isValueType = CMarshalNativeToManaged.IsValueType(array.Array.Type);
            support.WriteLine("{5} {0} = {4}mono_array_addr_with_size({1}, {2}, {3});",
                elementId, arrayId, elementSizeId, iteratorId,
                isValueType ? string.Empty : "*(MonoObject**)",
                isValueType ? "char*" : "MonoObject*");

            var ctx = new MarshalContext(Context.Context)
            {
                ArgName = elementId,
                ReturnVarName = elementId,
                ReturnType = array.Array.QualifiedType
            };

            var marshal = new CMarshalManagedToNative(Options, ctx) { UnboxPrimitiveValues = false };
            array.Array.QualifiedType.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                support.Write(marshal.Context.SupportBefore.ToString());

            support.WriteLine("g_array_append_val({0}.array, {1});", nativeArrayId,
                marshal.Context.Return.ToString());

            support.WriteCloseBraceIndent();

            Context.Return.Write("{0}", nativeArrayId);
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
            var objectId = $"{Context.ArgName}_obj";
            Context.SupportBefore.WriteLine("{1}* {0} = {2} ? ({1}*) mono_embeddinator_create_object({2}) : 0;",
                objectId, typeName, Context.ArgName);
            Context.Return.Write("{0}", objectId);
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
                case PrimitiveType.LongDouble:
                case PrimitiveType.Null:
                {
                    var typePrinter = CTypePrinter;
                    var typeName = Context.ReturnType.Visit(typePrinter);

                    var valueId = Context.ArgName;

                    if (UnboxPrimitiveValues)
                    {
                        var unboxId = CGenerator.GenId("unbox");
                        Context.SupportBefore.WriteLine("void* {0} = mono_object_unbox({1});",
                            unboxId, Context.ArgName);
                        valueId = unboxId;
                    }

                    Context.Return.Write("*(({0}*){1})", typeName, valueId);
                    return true;
                }
                case PrimitiveType.String:
                {
	                var stringId = CGenerator.GenId("string");
	                Context.SupportBefore.WriteLine("char* {0} = mono_string_to_utf8(" +
	                    "(MonoString*) {1});", stringId, Context.ArgName);
	
	                Context.Return.Write("{0}", stringId);
	                return true;
                }
            }

            throw new System.NotImplementedException(primitive.ToString());
        }
    }

    public class CMarshalNativeToManaged : CMarshalPrinter
    {
        public Options Options { get; private set; }
        public bool PrimitiveValuesByValue { get; set; }

        public CMarshalNativeToManaged (Options options, MarshalContext marshalContext)
            : base (marshalContext)
        {
            Options = options;
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
                case PrimitiveType.WideChar:
                    return "mono_get_char_class()";
                case PrimitiveType.Char:
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

                var @namespace = string.Empty;
                var ids = string.Join(", ",
                    decl.QualifiedName.Split('.').Select(n => $"\"{n}\""));

                var unit = decl.TranslationUnit;

                var classId = $"class_{decl.QualifiedName}";
                var monoImageName = $"{CGenerator.AssemblyId(unit)}_image";
                gen.WriteLine("{0} = mono_class_from_name({1}, \"{2}\", \"{3}\");",
                    classId, monoImageName, @namespace, decl.ManagedQualifiedName());

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
            var support = Context.SupportBefore;

            var contextId = CGenerator.GenId("mono_context");
            var arrayId = CGenerator.GenId($"{Context.ArgName}_array");
            var elementClassId = CGenerator.GenId($"{Context.ArgName}_element_class");

            var managedArray = array.Array;
            var elementType = managedArray.Type;
            support.WriteLine("MonoClass* {0} = mono_class_get_element_class({1});",
                elementClassId, GenerateArrayTypeLookup(elementType, support));

            support.WriteLine("MonoArray* {0} = mono_array_new({1}.domain, {2}, {3}.array->len);",
                arrayId, contextId, elementClassId, Context.ArgName);

            var iteratorId = CGenerator.GenId("i");
            support.WriteLine("for (int {0} = 0; {0} < {1}.array->len; {0}++)",
                              iteratorId, Context.ArgName);
            support.WriteStartBraceIndent();

            var typePrinter = CTypePrinter;
            string elementTypeName = elementType.Visit(typePrinter);

            var elementId = CGenerator.GenId($"{Context.ArgName}_array_element");
            support.WriteLine("{0} {1} = g_array_index({2}.array, {0}, {3});",
                elementTypeName, elementId, Context.ArgName, iteratorId);

            var ctx = new MarshalContext(Context.Context)
            {
                ArgName = elementId,
            };

            var marshal = new CMarshalNativeToManaged (Options, ctx) { PrimitiveValuesByValue = true };
            elementType.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                support.Write(marshal.Context.SupportBefore.ToString());

            var isValueType = IsValueType(elementType);
            if (isValueType)
            {
                support.WriteLine("mono_array_set({0}, {1}, {2}, {3});",
                    arrayId, elementTypeName, iteratorId,
                    marshal.Context.Return.ToString());
            }
            else
            {
                support.WriteLine("mono_array_setref({0}, {1}, {2});",
                    arrayId, iteratorId, marshal.Context.Return.ToString());
            }

            support.WriteCloseBraceIndent();

            Context.Return.Write("{0}", arrayId);

            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            Context.Return.Write("&{0}", Context.ArgName);
            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            var handle = CSources.GetMonoObjectField(Options, CSources.MonoObjectFieldUsage.Parameter,
                Context.ArgName, "_handle");

            var @object = $"{Context.ArgName} ? mono_gchandle_get_target({handle}) : 0";

            if (@class.IsValueType)
                @object = $"mono_object_unbox({@object})";

            Context.Return.Write("{0}", @object);
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
            var newArgName = CGenerator.GenId(Context.ArgName);
            Context.SupportBefore.WriteLine("{0} {1} = *(({0}*) {2});",
                integerType, newArgName, Context.ArgName);
            Context.Return.Write("&{0}", newArgName);
            Context.SupportAfter.WriteLine("*{0} = ({1}) {2};", Context.ArgName,
                @enum.QualifiedName, newArgName);
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

                Context.Return.Write("{0}", Context.ArgName);
                return true;
            }

            if (pointee is ArrayType)
            {
                // TODO: Handle out/ref array types.
                Context.Return.Write("0");
                return true;
            }

            return base.VisitPointerType(pointer, quals);
        }

        public bool VisitPrimitiveType(PrimitiveType primitive)
        {
            var param = Context.Parameter;
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
                case PrimitiveType.LongDouble:
                case PrimitiveType.Null:
                {
                    var prefix = "&";
                    if ((param != null && (param.IsInOut || param.IsOut))
                        || PrimitiveValuesByValue)
                        prefix = string.Empty;
                    Context.Return.Write ("{0}{1}", prefix, Context.ArgName);
                    return true;
                }
                case PrimitiveType.String:
                {
                    var argId = $"{CGenerator.GenId(Context.ArgName)}_{Context.ParameterIndex}";
                    var contextId = CGenerator.GenId("mono_context");
                    var @string = Context.ArgName;

                    var isByRef = param != null && (param.IsOut || param.IsInOut);
                    if (isByRef)
                    {
                        @string = $"{Context.ArgName}->str";
                        Context.SupportAfter.WriteLine("mono_embeddinator_marshal_string_to_gstring({0}, {1});",
                            Context.ArgName, argId);
                    }

                    Context.SupportBefore.WriteLine("MonoString* {0} = ({2}) ? mono_string_new({1}.domain, {2}) : 0;",
                        argId, contextId, @string);
                    Context.Return.Write("{0}{1}", isByRef ? "&" : string.Empty, argId);
                    return true;
                }
            }

            throw new System.NotImplementedException(primitive.ToString());
        }
    }
}
