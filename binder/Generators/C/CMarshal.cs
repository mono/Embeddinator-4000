using CppSharp.AST;
using CppSharp.AST.Extensions;
using System.Linq;

namespace MonoManagedToNative.Generators
{
    public class CMarshalPrinter : MarshalPrinter<MarshalContext>
    {
        public CMarshalPrinter(MarshalContext marshalContext)
            : base(marshalContext)
        {
        }

        public CManagedToNativeTypePrinter CTypePrinter
        {
            get
            {
                return CGenerator.GetCTypePrinter(
                    CppSharp.Generators.GeneratorKind.C);
            }
        }

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

        public CMarshalManagedToNative(MarshalContext marshalContext)
            : base(marshalContext)
        {
            UnboxPrimitiveValues = true;
        }

        public override bool VisitManagedArrayType(ManagedArrayType array,
            TypeQualifiers quals)
        {
            var support = Context.SupportBefore;

            var arrayId = CGenerator.GenId(string.Format("{0}_array",
                Context.ArgName));
            support.WriteLine("MonoArray* {0} = (MonoArray*) {1};",
                                            arrayId, Context.ArgName);

            var arraySizeId = CGenerator.GenId(string.Format("{0}_array_size",
                Context.ArgName));
            support.WriteLine("uintptr_t {0} = mono_array_length({1});",
                                            arraySizeId, arrayId);

            var typePrinter = CTypePrinter;
            typePrinter.PrintScopeKind = CppTypePrintScopeKind.Local;
            var arrayTypedefName = array.Typedef.Visit(typePrinter);

            typePrinter.PrintScopeKind = CppTypePrintScopeKind.Qualified;
            var arrayElementName = array.Array.Type.Visit(typePrinter);
            var elementSize = string.Format("sizeof({0})", arrayElementName);

            var nativeArrayId = CGenerator.GenId(string.Format("{0}_native_array",
                Context.ArgName));
            support.WriteLine("{0} {1};", arrayTypedefName, nativeArrayId);
            support.WriteLine("{0}.array = g_array_sized_new(/*zero_terminated=*/FALSE," +
                " /*clear_=*/TRUE, {1}, {2});", nativeArrayId, elementSize, arraySizeId);

            var elementClassId = CGenerator.GenId(string.Format("{0}_element_class",
                Context.ArgName));
            support.WriteLine("MonoClass* {0} = mono_class_get_element_class({1});",
                elementClassId,
                CMarshalNativeToManaged.GenerateArrayTypeLookup(array.Array.Type, support));

            var elementSizeId = CGenerator.GenId(string.Format("{0}_array_element_size",
                Context.ArgName));
            support.WriteLine("gint32 {0} = mono_class_array_element_size({1});",
                elementSizeId, elementClassId);

            var iteratorId = CGenerator.GenId("i");
            support.WriteLine("for (int {0} = 0; {0} < {1}; {0}++)",
                iteratorId, arraySizeId);
            support.WriteStartBraceIndent();

            var elementId = CGenerator.GenId(string.Format("{0}_array_element",
                Context.ArgName));

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

            var marshal = new CMarshalManagedToNative(ctx) { UnboxPrimitiveValues = false };
            array.Array.QualifiedType.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                support.Write(marshal.Context.SupportBefore.ToString());

            support.WriteLine("g_array_append_val({0}.array, {1});", nativeArrayId,
                marshal.Context.Return.ToString());

            support.WriteCloseBraceIndent();

            Context.Return.Write("{0}", nativeArrayId);
            return false;
        }

        bool HandleSpecialCILType(CILType cilType)
        {
            var type = cilType.Type;

            if (type == typeof(string))
            {
                var stringId = CGenerator.GenId("string");
                Context.SupportBefore.WriteLine("char* {0} = mono_string_to_utf8(" +
                    "(MonoString*) {1});", stringId, Context.ArgName);

                Context.Return.Write("{0}", stringId);
                return true;
            }

            return false;
        }

        public override bool VisitClassDecl(Class @class)
        {
            var typeName = @class.Visit(CTypePrinter);
            var objectId = string.Format("{0}_obj", Context.ArgName);
            Context.SupportBefore.WriteLine("{1}* {0} = ({1}*) mono_m2n_create_object({2});",
                objectId, typeName, Context.ArgName);
            Context.Return.Write("{0}", objectId);
            return true;
        }

        public override bool VisitCILType(CILType type, TypeQualifiers quals)
        {
            if (HandleSpecialCILType(type))
                return true;

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

            throw new System.NotSupportedException();
        }
    }

    public class CMarshalNativeToManaged : CMarshalPrinter
    {
        public bool PrimitiveValuesByValue { get; set; }

        public CMarshalNativeToManaged (MarshalContext marshalContext)
            : base (marshalContext)
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

                return string.Format("mono_m2n_search_class(\"{0}\", \"{1}\", \"{2}\")",
                    cilType.Type.Assembly.GetName().Name, cilType.Type.Namespace,
                    cilType.Type.Name);
            }
            else if (type is TagType)
            {
                var tagType = type as TagType;
                var decl = tagType.Declaration;

                var @namespace = string.Empty;
                var ids = string.Join(", ",
                    decl.QualifiedName.Split('.').Select(n => string.Format("\"{0}\"", n)));

                var unit = decl.TranslationUnit;

                var classId = string.Format("{0}_class", decl.QualifiedName);
                var monoImageName = string.Format("{0}_image", unit.Name);
                gen.WriteLine("{0} = mono_class_from_name({1}, \"{2}\", \"{3}\");",
                    classId, monoImageName, @namespace, decl.OriginalName);

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

            if (type is BuiltinType)
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
            var arrayId = CGenerator.GenId(string.Format("{0}_array", Context.ArgName));
            var elementClassId = CGenerator.GenId(string.Format("{0}_element_class",
                Context.ArgName));

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

            var elementId = CGenerator.GenId(string.Format("{0}_array_element",
                Context.ArgName));

            support.WriteLine("{0} {1} = g_array_index({2}.array, {0}, {3});",
                elementTypeName, elementId, Context.ArgName, iteratorId);

            var ctx = new MarshalContext(Context.Context)
            {
                ArgName = elementId,
            };


            var marshal = new CMarshalNativeToManaged (ctx) { PrimitiveValuesByValue = true };
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

        public override bool VisitClassDecl(Class @class)
        {
            var instanceId = CGenerator.GenId(string.Format("{0}_instance", Context.ArgName));
            Context.SupportBefore.WriteLine("MonoObject* {0} = mono_gchandle_get_target({1}->_handle);",
                instanceId, Context.ArgName);
            Context.Return.Write("{0}", instanceId);
            return true;
        }

        bool HandleSpecialCILType(CILType cilType)
        {
            var type = cilType.Type;

            if (type == typeof(string))
            {
                var argId = CGenerator.GenId(Context.ArgName);
                var contextId = CGenerator.GenId("mono_context");
                var stringText = Context.ArgName;

                var param = Context.Parameter;
                var isByRef = param != null && (param.IsOut || param.IsInOut);
                if (isByRef)
                {
                    stringText = string.Format ("({0}->len != 0) ? {0}->str : \"\"",
                        Context.ArgName);

                    Context.SupportAfter.WriteLine ("g_string_truncate({0}, 0);", Context.ArgName);
                    Context.SupportAfter.WriteLine ("g_string_append({0}, mono_string_to_utf8(" +
                        "(MonoString*) {1}));", Context.ArgName, argId);
                }

                Context.SupportBefore.WriteLine("MonoString* {0} = mono_string_new({1}.domain, {2});",
                    argId, contextId, stringText);
                Context.Return.Write("{0}{1}", isByRef ? "&" : string.Empty, argId);
                return true;
            }

            return false;
        }

        public override bool VisitCILType(CILType type, TypeQualifiers quals)
        {
            if (HandleSpecialCILType(type))
                return true;

            return base.VisitCILType(type, quals);
        }

        public override bool VisitBuiltinType(BuiltinType builtin,
            TypeQualifiers quals)
        {
            return VisitPrimitiveType(builtin.Type);
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
                    var prefix = "&";
                    if ((param != null && (param.IsInOut || param.IsOut))
                        || PrimitiveValuesByValue)
                        prefix = string.Empty;
                    Context.Return.Write ("{0}{1}", prefix, Context.ArgName);
                    return true;
            }

            throw new System.NotSupportedException();
        }
    }
}
