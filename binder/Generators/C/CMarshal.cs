using CppSharp.AST;
using CppSharp.Types;

namespace MonoManagedToNative.Generators
{
    public static class TypeExtensions
    {
        public static void CMarshalToNative(this QualifiedType type,
            CMarshalManagedToNative printer)
        {
            type.Visit(printer);
        }
    }

    public class CMarshalManagedToNative : MarshalPrinter<MarshalContext>
    {
        public CMarshalManagedToNative(MarshalContext marshalContext)
            : base(marshalContext)
        {
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
                    // Unbox MonoObject to get at the primitive value.
                    var unboxId = CGenerator.GenId("unbox");
                    Context.SupportBefore.WriteLine("void* {0} = mono_object_unbox({1});",
                        unboxId, Context.ArgName);
                    var typePrinter = new CppTypePrinter();
                    string typeName = Context.ReturnType.Visit(typePrinter);

                    Context.Return.Write("*(({0}*){1})", typeName, unboxId);
                    return true;
            }

            throw new System.NotSupportedException();
        }
    }
}
