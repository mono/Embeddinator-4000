using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

using Embeddinator;
namespace ObjC {
	public partial class ObjCProcessor {
	
		public static string GetArrayCreator (string parameterName, Type type)
		{
			string arrayCreator = $"MonoArray* __{parameterName}arr = mono_array_new (__mono_context.domain, {{0}}, __{parameterName}length);";

			switch (Type.GetTypeCode (type)) {
			case TypeCode.String:
				return string.Format (arrayCreator, "mono_get_string_class ()");
			case TypeCode.Boolean:
				return string.Format (arrayCreator, "mono_get_boolean_class ()");
			case TypeCode.Char:
				return string.Format (arrayCreator, "mono_get_char_class ()");
			case TypeCode.SByte:
				return string.Format (arrayCreator, "mono_get_sbyte_class ()");
			case TypeCode.Int16:
				return string.Format (arrayCreator, "mono_get_int16_class ()");
			case TypeCode.Int32:
				return string.Format (arrayCreator, "mono_get_int32_class ()");
			case TypeCode.Int64:
				return string.Format (arrayCreator, "mono_get_int64_class ()");
			case TypeCode.Byte:
				return string.Format (arrayCreator, "mono_get_byte_class ()");
			case TypeCode.UInt16:
				return string.Format (arrayCreator, "mono_get_uint16_class ()");
			case TypeCode.UInt32:
				return string.Format (arrayCreator, "mono_get_uint32_class ()");
			case TypeCode.UInt64:
				return string.Format (arrayCreator, "mono_get_uint64_class ()");
			case TypeCode.Single:
				return string.Format (arrayCreator, "mono_get_single_class ()");
			case TypeCode.Double:
				return string.Format (arrayCreator, "mono_get_double_class ()");
			case TypeCode.Object:
				return string.Format (arrayCreator, $"{NameGenerator.GetObjCName (type)}_class");
			case TypeCode.Decimal:
				return string.Format (arrayCreator, "mono_embeddinator_get_decimal_class ()");
			default:
				throw new NotImplementedException ($"Converting type {type.FullName} to mono class");
			}
		}
	}
}
