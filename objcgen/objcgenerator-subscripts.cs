using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

using Embeddinator;

namespace ObjC
{
	public partial class ObjCGenerator
	{
		protected void GenerateSubscript (PropertyInfo pi)
		{
			Type indexType = pi.GetSetMethod ().GetParameters ()[0].ParameterType;
			Type paramType = pi.PropertyType;
			switch (Type.GetTypeCode (indexType)) {
			case TypeCode.Byte:
			case TypeCode.SByte:
			case TypeCode.UInt16:
			case TypeCode.UInt32:
			case TypeCode.UInt64:
			case TypeCode.Int16:
			case TypeCode.Int32:
				GenerateIndexedSubscripting (indexType, paramType);
				return;
			default:
				GenerateKeyedSubscripting (paramType);
				return;
			}
		}

		protected void GenerateKeyedSubscripting (Type paramType)
		{
		}

		protected void GenerateIndexedSubscripting (Type indexType, Type propertyType)
		{
			string indexTypeString = GetTypeName (indexType);

			headers.WriteLine ($"- (id)objectAtIndexedSubscript:({indexTypeString})idx;");

			implementation.WriteLine ($"- (id)objectAtIndexedSubscript:({indexTypeString})idx");
			implementation.WriteLine ("{");
			implementation.WriteLine ($"\treturn {ToNSNumber (propertyType, "[self getItem:idx]")};");
			implementation.WriteLine ("}");
			implementation.WriteLine ();

			headers.WriteLine ($"- (void)setObject:(id)obj atIndexedSubscript: ({indexTypeString})idx;");

			implementation.WriteLine ($"- (void)setObject:(id)obj atIndexedSubscript: ({indexTypeString})idx");
			implementation.WriteLine ("{");
			implementation.WriteLine ($"\t[self setItem:idx value:{FromNSNumber (propertyType, "obj")}];");
			implementation.WriteLine ("}");
			implementation.WriteLine ();
		}

		internal string ToNSNumber (Type type, string code)
		{
			switch (Type.GetTypeCode (type)) {
			case TypeCode.Boolean:
				return $"[[NSNumber alloc] initWithBool: {code}]";
			case TypeCode.SByte:
				return $"[[NSNumber alloc] initWithChar: {code}]";
			case TypeCode.Byte:
				return $"[[NSNumber alloc] initWithUnsignedChar: {code}]";
			case TypeCode.Int16:
				return $"[[NSNumber alloc] initWithShort: {code}]";
			case TypeCode.UInt16:
				return $"[[NSNumber alloc] initWithUnsignedShort: {code}]";
			case TypeCode.Int32:
				return $"[[NSNumber alloc] initWithInt: {code}]";
			case TypeCode.UInt32:
				return $"[[NSNumber alloc] initWithUnsignedInt: {code}]";
			case TypeCode.Int64:
				return $"[[NSNumber alloc] initWithLongLong: {code}]";
			case TypeCode.UInt64:
				return $"[[NSNumber alloc] initWithUnsignedLong: {code}]";
			case TypeCode.Single:
				return $"[[NSNumber alloc] initWithFloat: {code}]";
			case TypeCode.Double:
				return $"[[NSNumber alloc] initWithDouble: {code}]";
			case TypeCode.Char:
				return $"[[NSNumber alloc] initWithUnsignedChar: {code}]"; // TODO - Not sure on this
			case TypeCode.String:
			case TypeCode.Object:
				return code;
			default:
				throw new NotSupportedException ();
			}
		}

		internal string FromNSNumber (Type type, string code)
		{
			switch (Type.GetTypeCode (type)) {
			case TypeCode.Boolean:
				return $"[{code} boolValue]";
			case TypeCode.SByte:
				return $"[{code} charValue]";
			case TypeCode.Byte:
				return $"[{code} unsignedCharValue]";
			case TypeCode.Int16:
				return $"[{code} shortValue]";
			case TypeCode.UInt16:
				return $"[{code} unsignedShortValue]";
			case TypeCode.Int32:
				return $"[{code} intValue]";
			case TypeCode.UInt32:
				return $"[{code} unsignedIntValue]";
			case TypeCode.Int64:
				return $"[{code} longLongValue]";
			case TypeCode.UInt64:
				return $"[{code} unsignedLongLongValue]";
			case TypeCode.Single:
				return $"[{code} floatValue]";
			case TypeCode.Double:
				return $"[{code} doubleValue]";
			case TypeCode.Char:
				return $"[{code} unsignedCharValue]"; // TODO - Not sure on this
			case TypeCode.String:
			case TypeCode.Object:
				return code;
			default:
				throw new NotSupportedException ();
			}
		}
	}
}
