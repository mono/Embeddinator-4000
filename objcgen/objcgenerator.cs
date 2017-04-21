﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

using Embeddinator;

namespace ObjC {
	
	public partial class ObjCGenerator : Generator {

		TextWriter headers = new StringWriter ();
		TextWriter implementation = new StringWriter ();

		public override void Generate (IEnumerable<Assembly> assemblies)
		{
			headers.WriteLine ("#include \"embeddinator.h\"");
			headers.WriteLine ("#import <Foundation/Foundation.h>");
			headers.WriteLine ();
			headers.WriteLine ();
			headers.WriteLine ("#if !__has_feature(objc_arc)");
			headers.WriteLine ("#error Embeddinator code must be built with ARC.");
			headers.WriteLine ("#endif");
			headers.WriteLine ();

			headers.WriteLine ("// forward declarations");
			foreach (var t in types)
				headers.WriteLine ($"@class {GetTypeName (t)};");
			headers.WriteLine ();
			headers.WriteLine ("NS_ASSUME_NONNULL_BEGIN");
			headers.WriteLine ();

			implementation.WriteLine ("#include \"bindings.h\"");
			implementation.WriteLine ("#include \"glib.h\"");
			implementation.WriteLine ("#include \"objc-support.h\"");
			implementation.WriteLine ("#include \"mono_embeddinator.h\"");
			implementation.WriteLine ("#include \"mono-support.h\"");
			implementation.WriteLine ();

			implementation.WriteLine ("mono_embeddinator_context_t __mono_context;");
			implementation.WriteLine ();

			foreach (var a in assemblies)
				implementation.WriteLine ($"MonoImage* __{SanitizeName (a.GetName ().Name)}_image;");
			implementation.WriteLine ();

			foreach (var t in types)
				implementation.WriteLine ($"static MonoClass* {GetObjCName (t)}_class = nil;");
			implementation.WriteLine ();

			implementation.WriteLine ("static void __initialize_mono ()");
			implementation.WriteLine ("{");
			implementation.WriteLine ("\tif (__mono_context.domain)");
			implementation.WriteLine ("\t\treturn;");
			implementation.WriteLine ("\tmono_embeddinator_init (&__mono_context, \"mono_embeddinator_binding\");");
			implementation.WriteLine ("}");
			implementation.WriteLine ();

			base.Generate (assemblies);

			headers.WriteLine ("NS_ASSUME_NONNULL_END");
			headers.WriteLine ();
		}

		protected override void Generate (Assembly a)
		{
			var originalName = a.GetName ().Name;
			var name = SanitizeName (originalName);
			implementation.WriteLine ($"static void __lookup_assembly_{name} ()");
			implementation.WriteLine ("{");
			implementation.WriteLine ($"\tif (__{name}_image)");
			implementation.WriteLine ("\t\treturn;");
			implementation.WriteLine ("\t__initialize_mono ();");
			implementation.WriteLine ($"\t__{name}_image = mono_embeddinator_load_assembly (&__mono_context, \"{originalName}.dll\");");
			implementation.WriteLine ($"\tassert (__{name}_image && \"Could not load the assembly '{originalName}.dll'.\");");
			var categories = extensions_methods.Keys;
			if (categories.Count > 0) {
				implementation.WriteLine ("\t// we cannot use `+initialize` inside categories as they would replace the original type code");
				implementation.WriteLine ("\t// since there should not be tons of them we're pre-loading them when loading the assembly");
				foreach (var definedType in extensions_methods.Keys) {
					var managed_name = GetObjCName (definedType);
					implementation.WriteLine ("#if TOKENLOOKUP");
					implementation.WriteLine ($"\t{managed_name}_class = mono_class_get (__{name}_image, 0x{definedType.MetadataToken:X8});");
					implementation.WriteLine ("#else");
					implementation.WriteLine ($"\t{managed_name}_class = mono_class_from_name (__{name}_image, \"{definedType.Namespace}\", \"{definedType.Name}\");");
					implementation.WriteLine ("#endif");
				}
			}

			implementation.WriteLine ("}");
			implementation.WriteLine ();

			foreach (var t in enums) {
				GenerateEnum (t);
			}

			foreach (var t in types) {
				Generate (t);
			}

			foreach (var extension in extensions_methods) {
				var defining_type = extension.Key;
				foreach (var category in extension.Value)
					GenerateCategory (defining_type, category.Key, category.Value);
			}
		}

		void GenerateCategory (Type definedType, Type extendedType, List<MethodInfo> methods)
		{
			var etn = GetTypeName (extendedType).Replace (" *", String.Empty);
			var name = $"{etn} ({GetTypeName (definedType)})";
			headers.WriteLine ($"/** Category {name}");
			headers.WriteLine ($" *  Corresponding .NET Qualified Name: `{definedType.AssemblyQualifiedName}`");
			headers.WriteLine (" */");
			headers.WriteLine ($"@interface {name}");
			headers.WriteLine ();

			implementation.WriteLine ($"@implementation {name}");
			implementation.WriteLine ();

			foreach (var mi in methods) {
				ImplementMethod (mi, CamelCase (mi.Name), true);
			}

			headers.WriteLine ("@end");
			headers.WriteLine ();

			implementation.WriteLine ("@end");
			implementation.WriteLine ();
		}

		void GenerateEnum (Type t)
		{
			var managed_name = GetObjCName (t);
			var underlying_type = t.GetEnumUnderlyingType ();
			var base_type = GetTypeName (underlying_type);

			// it's nicer to expose flags as unsigned integers - but .NET defaults to `int`
			bool flags = t.HasCustomAttribute ("System", "FlagsAttribute");
			if (flags) {
				switch (Type.GetTypeCode (underlying_type)) {
				case TypeCode.SByte:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
					base_type = "unsigned " + base_type;
					break;
				}
			}

			var macro = flags ? "NS_OPTIONS" : "NS_ENUM";
			headers.WriteLine ($"/** Enumeration {managed_name}");
			headers.WriteLine ($" *  Corresponding .NET Qualified Name: `{t.AssemblyQualifiedName}`");
			headers.WriteLine (" */");
			headers.WriteLine ($"typedef {macro}({base_type}, {managed_name}) {{");
			foreach (var name in t.GetEnumNames ()) {
				var value = t.GetField (name).GetRawConstantValue ();
				headers.Write ($"\t{managed_name}{name} = ");
				if (flags)
					headers.Write ($"0x{value:x}");
				else
					headers.Write (value);
				headers.WriteLine (',');
			}
			headers.WriteLine ("};");
			headers.WriteLine ();
		}

		protected override void Generate (Type t)
		{
			var has_bound_base_class = types.Contains (t.BaseType);
			var static_type = t.IsSealed && t.IsAbstract;

			var managed_name = GetObjCName (t);

			var native_name = GetTypeName (t);
			headers.WriteLine ();
			headers.WriteLine ($"/** Class {native_name}");
			headers.WriteLine ($" *  Corresponding .NET Qualified Name: `{t.AssemblyQualifiedName}`");
			headers.WriteLine (" */");
			headers.WriteLine ($"@interface {native_name} : {GetTypeName (t.BaseType)} {{");
			if (!static_type && !has_bound_base_class) {
				headers.WriteLine ("\t@public MonoEmbedObject* _object;");
			}
			headers.WriteLine ("}");
			headers.WriteLine ();

			implementation.WriteLine ();
			implementation.WriteLine ($"/** Class {native_name}");
			implementation.WriteLine ($" *  Corresponding .NET Qualified Name: `{t.AssemblyQualifiedName}`");
			implementation.WriteLine (" */");
			implementation.WriteLine ($"@implementation {native_name} {{");
			implementation.WriteLine ("}");
			implementation.WriteLine ();

			implementation.WriteLine ("+ (void) initialize");
			implementation.WriteLine ("{");
			implementation.WriteLine ($"\tif (self != [{managed_name} class])");
			implementation.WriteLine ("\t\treturn;");
			var aname = SanitizeName (t.Assembly.GetName ().Name);
			implementation.WriteLine ($"\t__lookup_assembly_{aname} ();");

			implementation.WriteLine ("#if TOKENLOOKUP");
			implementation.WriteLine ($"\t{managed_name}_class = mono_class_get (__{aname}_image, 0x{t.MetadataToken:X8});");
			implementation.WriteLine ("#else");
			implementation.WriteLine ($"\t{managed_name}_class = mono_class_from_name (__{aname}_image, \"{t.Namespace}\", \"{t.Name}\");");
			implementation.WriteLine ("#endif");
			implementation.WriteLine ("}");
			implementation.WriteLine ();

			if (!static_type && !has_bound_base_class) {
				implementation.WriteLine ("-(void) dealloc");
				implementation.WriteLine ("{");
				implementation.WriteLine ("\tif (_object)");
				implementation.WriteLine ("\t\tmono_embeddinator_destroy_object (_object);");
				implementation.WriteLine ("}");
				implementation.WriteLine ("");
			}

			var default_init = false;
			List<ConstructorInfo> constructors;
			if (ctors.TryGetValue (t, out constructors)) {
				// First get the unavailable init ctor selectors in parent class
				var unavailableCtors = GetUnavailableParentCtors (t, constructors);
				if (unavailableCtors.Count () > 0) {
					// TODO: Print a #pragma mark once we have a well defined header structure http://nshipster.com/pragma/
					foreach (var uctor in unavailableCtors) {
						var ctorparams = uctor.GetParameters ();
						string name = "init";
						string signature = ".ctor()";
						if (ctorparams.Length > 0)
							GetSignatures ("initWith", uctor.Name, uctor, ctorparams, false, out name, out signature);
						headers.WriteLine ("/** This initializer is not available as it was not re-exposed from the base type");
						headers.WriteLine (" *  For more details consult https://github.com/mono/Embeddinator-4000/blob/master/docs/ObjC.md#constructors-vs-initializers");
						headers.WriteLine (" */");
						headers.WriteLine ($"- (nullable instancetype){name} NS_UNAVAILABLE;");
						headers.WriteLine ();
					}
				}

				foreach (var ctor in constructors) {
					var pcount = ctor.ParameterCount;
					default_init |= pcount == 0;

					var parameters = ctor.GetParameters ();
					string name = "init";
					string signature = ".ctor()";
					if (parameters.Length > 0)
						GetSignatures ("initWith", ctor.Name, ctor, parameters, false, out name, out signature);

					var builder = new MethodHelper (headers, implementation) {
						AssemblyName = aname,
						ReturnType = "nullable instancetype",
						ManagedTypeName = t.FullName,
						MetadataToken = ctor.MetadataToken,
						MonoSignature = signature,
						ObjCSignature = name,
						ObjCTypeName = managed_name,
						IsConstructor = true,
						IsValueType = t.IsValueType,
						IgnoreException = true,
					};

					builder.WriteHeaders ();

					builder.BeginImplementation ();
					builder.WriteMethodLookup ();

					// TODO: this logic will need to be update for managed NSObject types (e.g. from XI / XM) not to call [super init]
					implementation.WriteLine ("\tif (!_object) {");
					implementation.WriteLine ($"\t\tMonoObject* __instance = mono_object_new (__mono_context.domain, {managed_name}_class);");

					string postInvoke = String.Empty;
					var args = "nil";
					if (pcount > 0) {
						Generate (parameters, false, out postInvoke);
						args = "__args";
					}
					builder.WriteInvoke (args);
					implementation.Write (postInvoke);
					implementation.WriteLine ("\t\t_object = mono_embeddinator_create_object (__instance);");
					implementation.WriteLine ("\t}");
					if (types.Contains (t.BaseType))
						implementation.WriteLine ("\treturn self = [super initForSuper];");
					else
						implementation.WriteLine ("\treturn self = [super init];");
					builder.EndImplementation ();

					headers.WriteLine ();
				}
			}

			if (!default_init || static_type) {
				if (static_type) {
					headers.WriteLine ("/** This is a static type and no instance can be initialized");
				} else {
					headers.WriteLine ("/** This type is not meant to be created using only default values");
				}
				headers.WriteLine (" *  Both the `-init` and `+new` selectors cannot be used to create instances of this type.");
				headers.WriteLine (" */");
				headers.WriteLine ("- (nullable instancetype)init NS_UNAVAILABLE;");
				headers.WriteLine ("+ (nullable instancetype)new NS_UNAVAILABLE;");
				headers.WriteLine ();
			}

			// TODO we should re-use the base `init` when it exists
			if (!static_type) {
				headers.WriteLine ("/** This selector is not meant to be called from user code");
				headers.WriteLine (" *  This exists solely to allow the correct subclassing of managed (.net) types");
				headers.WriteLine (" */");
				headers.WriteLine ("- (nullable instancetype)initForSuper;");

				implementation.WriteLine ("- (nullable instancetype) initForSuper {");
				// calls super's initForSuper until we reach a non-generated type
				if (types.Contains (t.BaseType))
					implementation.WriteLine ("\treturn self = [super initForSuper];");
				else
					implementation.WriteLine ("\treturn self = [super init];");
				implementation.WriteLine ("}");
				implementation.WriteLine ();
			}

			List<PropertyInfo> props;
			if (properties.TryGetValue (t, out props)) {
				headers.WriteLine ();
				foreach (var pi in props)
					Generate (pi);
			}

			List<FieldInfo> f;
			if (fields.TryGetValue (t, out f)) {
				headers.WriteLine ();
				foreach (var fi in f)
					Generate (fi);
			}

			List<PropertyInfo> s;
			if (subscriptProperties.TryGetValue (t, out s)) {
				headers.WriteLine ();
				foreach (var si in s)
					GenerateSubscript (si);
			}

			List<MethodInfo> meths;
			if (methods.TryGetValue (t, out meths)) {
				headers.WriteLine ();
				foreach (var mi in meths)
					Generate (mi);
			}

			MethodInfo m;
			if (icomparable.TryGetValue (t, out m)) {
				var pt = m.GetParameters () [0].ParameterType;
				var builder = new ComparableHelper (headers, implementation) {
					ObjCSignature = $"compare:({managed_name} * _Nullable)other",
					AssemblyName = aname,
					MetadataToken = m.MetadataToken,
					ObjCTypeName = managed_name,
					ManagedTypeName = t.FullName,
					MonoSignature = $"CompareTo({GetMonoName (pt)})",
				};
				builder.WriteHeaders ();
				builder.WriteImplementation ();
			}

			headers.WriteLine ("@end");
			headers.WriteLine ();

			implementation.WriteLine ("@end");
			implementation.WriteLine ();
		}

		void Generate (ParameterInfo [] parameters, bool isExtension, out string postInvoke)
		{
			StringBuilder post = new StringBuilder ();
			var pcount = parameters.Length;
			implementation.WriteLine ($"\t\tvoid* __args [{pcount}];");
			for (int i = 0; i < pcount; i++) {
				var p = parameters [i];
				var name = (isExtension && (i == 0)) ? "self" : p.Name;
				GenerateArgument (name, $"__args[{i}]", p.ParameterType, ref post);
			}
			postInvoke = post.ToString ();
		}

		void GenerateArgument (string paramaterName, string argumentName, Type t, ref StringBuilder post)
		{
			var is_by_ref = t.IsByRef;
			if (is_by_ref)
				t = t.GetElementType ();
			
			switch (Type.GetTypeCode (t)) {
			case TypeCode.String:
				if (is_by_ref) {
					implementation.WriteLine ($"\t\tMonoString* __string = *{paramaterName} ? mono_string_new (__mono_context.domain, [*{paramaterName} UTF8String]) : nil;");
					implementation.WriteLine ($"\t\t{argumentName} = &__string;");
					post.AppendLine ($"\t\t*{paramaterName} = mono_embeddinator_get_nsstring (__string);");
				} else
					implementation.WriteLine ($"\t\t{argumentName} = {paramaterName} ? mono_string_new (__mono_context.domain, [{paramaterName} UTF8String]) : nil;");
				break;
			case TypeCode.Boolean:
			case TypeCode.Char:
			case TypeCode.SByte:
			case TypeCode.Int16:
			case TypeCode.Int32:
			case TypeCode.Int64:
			case TypeCode.Byte:
			case TypeCode.UInt16:
			case TypeCode.UInt32:
			case TypeCode.UInt64:
			case TypeCode.Single:
			case TypeCode.Double:
				if (is_by_ref)
					implementation.WriteLine ($"\t\t{argumentName} = {paramaterName};");
				else
					implementation.WriteLine ($"\t\t{argumentName} = &{paramaterName};");
				break;
			default:
				if (t.IsValueType)
					implementation.WriteLine ($"\t\t{argumentName} = mono_object_unbox (mono_gchandle_get_target ({paramaterName}->_object->_handle));");
				else 
				if (types.Contains (t))
					implementation.WriteLine ($"\t\t{argumentName} = {paramaterName} ? mono_gchandle_get_target ({paramaterName}->_object->_handle): nil;");
				else
					throw new NotImplementedException ($"Converting type {t.FullName} to mono code");
				break;
			}
		}

		protected override void Generate (PropertyInfo pi)
		{
			var getter = pi.GetGetMethod ();
			var setter = pi.GetSetMethod ();
			// setter-only properties are handled as methods (and should not reach this code)
			if (getter == null && setter != null)
				throw new EmbeddinatorException (99, "Internal error `setter only`. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues");

			var name = CamelCase (pi.Name);

			headers.Write ("@property (nonatomic");
			if (getter.IsStatic)
				headers.Write (", class");
			if (setter == null)
				headers.Write (", readonly");
			var pt = pi.PropertyType;
			var property_type = GetTypeName (pt);
			if (types.Contains (pt))
				property_type += " *";
			headers.WriteLine ($") {property_type} {name};");

			ImplementMethod (getter, name, false, pi);
			if (setter == null)
				return;

			ImplementMethod (setter, "set" + pi.Name, false, pi);
		}

		protected void Generate (FieldInfo fi)
		{
			bool read_only = fi.IsInitOnly || fi.IsLiteral;

			headers.Write ("@property (nonatomic");
			if (fi.IsStatic)
				headers.Write (", class");
			if (read_only)
				headers.Write (", readonly");
			var ft = fi.FieldType;
			var bound = types.Contains (ft);
			if (bound && ft.IsValueType)
				headers.Write (", nonnull");

			var field_type = GetTypeName (ft);
			if (bound)
				field_type += " *";

			var name = CamelCase (fi.Name);
			headers.WriteLine ($") {field_type} {name};");

			// it's similar, but different from implementing a method

			var type = fi.DeclaringType;
			var managed_type_name = GetObjCName (type);
			var return_type = GetReturnType (type, fi.FieldType);

			implementation.Write (fi.IsStatic ? '+' : '-');
			implementation.WriteLine ($" ({return_type}) {CamelCase (fi.Name)}");
			implementation.WriteLine ("{");
			implementation.WriteLine ("\tstatic MonoClassField* __field = nil;");
			implementation.WriteLine ("\tif (!__field) {");
			implementation.WriteLine ("#if TOKENLOOKUP");
			var aname = type.Assembly.GetName ().Name;
			implementation.WriteLine ($"\t\t__field = mono_class_get_field ({managed_type_name}_class, 0x{fi.MetadataToken:X8});");
			implementation.WriteLine ("#else");
			implementation.WriteLine ($"\t\tconst char __field_name [] = \"{fi.Name}\";");
			implementation.WriteLine ($"\t\t__field = mono_class_get_field_from_name ({managed_type_name}_class, __field_name);");
			implementation.WriteLine ("#endif");
			implementation.WriteLine ("\t}");
			var instance = "nil";
			if (!fi.IsStatic) {
				implementation.WriteLine ($"\tMonoObject* __instance = mono_gchandle_get_target (_object->_handle);");
				instance = "__instance";
			}
			implementation.WriteLine ($"\tMonoObject* __result = mono_field_get_value_object (__mono_context.domain, __field, {instance});");
			if (types.Contains (ft)) {
				implementation.WriteLine ("\tif (!__result)");
				implementation.WriteLine ("\t\treturn nil;");
			}
			ReturnValue (fi.FieldType);
			implementation.WriteLine ("}");
			implementation.WriteLine ();

			if (read_only)
				return;
			implementation.Write (fi.IsStatic ? '+' : '-');
			implementation.WriteLine ($" (void) set{fi.Name}:({field_type})value");
			implementation.WriteLine ("{");
			implementation.WriteLine ("\tstatic MonoClassField* __field = nil;");
			implementation.WriteLine ("\tif (!__field) {");
			implementation.WriteLine ("#if TOKENLOOKUP");
			aname = type.Assembly.GetName ().Name;
			implementation.WriteLine ($"\t\t__field = mono_class_get_field ({managed_type_name}_class, 0x{fi.MetadataToken:X8});");
			implementation.WriteLine ("#else");
			implementation.WriteLine ($"\t\tconst char __field_name [] = \"{fi.Name}\";");
			implementation.WriteLine ($"\t\t__field = mono_class_get_field_from_name ({managed_type_name}_class, __field_name);");
			implementation.WriteLine ("#endif");
			implementation.WriteLine ("\t}");
			StringBuilder sb = null;
			implementation.WriteLine ($"\t\tvoid* __value;");
			GenerateArgument ("value", "__value", fi.FieldType, ref sb);
			if (fi.IsStatic) {
				implementation.WriteLine ($"\tMonoVTable *__vtable = mono_class_vtable (__mono_context.domain, {managed_type_name}_class);");
				implementation.WriteLine ("\tmono_field_static_set_value (__vtable, __field, __value);");
			} else {
				implementation.WriteLine ($"\tMonoObject* __instance = mono_gchandle_get_target (_object->_handle);");
				implementation.WriteLine ("\tmono_field_set_value (__instance, __field, __value);");
			}
			implementation.WriteLine ("}");
			implementation.WriteLine ();
		}

		public string GetReturnType (Type declaringType, Type returnType)
		{
			if (declaringType == returnType)
				return "instancetype";

			var return_type = GetTypeName (returnType);
			if (types.Contains (returnType))
				return_type += "*";
			return return_type;
		}

		// TODO override with attribute ? e.g. [ObjC.Selector ("foo")]
		void ImplementMethod (MethodInfo info, string name, bool isExtension = false, PropertyInfo pi = null)
		{
			var type = info.DeclaringType;
			var managed_type_name = GetObjCName (type);

			string objcsig;
			string monosig;
			var managed_name = info.Name;
			var parametersInfo = info.GetParameters ();
			GetSignatures (name, managed_name, (MemberInfo)pi ?? info, parametersInfo, isExtension, out objcsig, out monosig);

			var builder = new MethodHelper (headers, implementation) {
				AssemblyName = type.Assembly.GetName ().Name,
				IsStatic = info.IsStatic,
				IsExtension = isExtension,
				ReturnType = GetReturnType (type, info.ReturnType),
				ManagedTypeName = type.FullName,
				MetadataToken = info.MetadataToken,
				MonoSignature = monosig,
				ObjCSignature = objcsig,
				ObjCTypeName = managed_type_name,
				IsValueType = type.IsValueType,
			};

			if (pi == null)
				builder.WriteHeaders ();
			
			builder.BeginImplementation ();
			builder.WriteMethodLookup ();

			string postInvoke = String.Empty;
			var args = "nil";
			if (parametersInfo.Length > 0) {
				Generate (parametersInfo, isExtension, out postInvoke);
				args = "__args";
			}

			builder.WriteInvoke (args);

			// ref and out parameters might need to be converted back
			implementation.Write (postInvoke);
			ReturnValue (info.ReturnType);
			builder.EndImplementation ();
		}

		protected override void Generate (MethodInfo mi)
		{
			string name;
			if (mi.IsSpecialName && mi.IsStatic && mi.Name.StartsWith ("op_", StringComparison.Ordinal))
				name = CamelCase (mi.Name.Substring (3));
			else
				name = CamelCase (mi.Name);
			ImplementMethod (mi, name);
		}

		void ReturnValue (Type t)
		{
			switch (Type.GetTypeCode (t)) {
			case TypeCode.String:
				implementation.WriteLine ("\treturn mono_embeddinator_get_nsstring ((MonoString *) __result);");
				break;
			case TypeCode.Boolean:
			case TypeCode.Char:
			case TypeCode.SByte:
			case TypeCode.Int16:
			case TypeCode.Int32:
			case TypeCode.Int64:
			case TypeCode.Byte:
			case TypeCode.UInt16:
			case TypeCode.UInt32:
			case TypeCode.UInt64:
			case TypeCode.Single:
			case TypeCode.Double:
				var name = GetTypeName (t);
				implementation.WriteLine ("\tvoid* __unbox = mono_object_unbox (__result);");
				implementation.WriteLine ($"\treturn *(({name}*)__unbox);");
				break;
			case TypeCode.Object:
				if (t.Namespace == "System" && t.Name == "Void")
					return;
				if (!types.Contains (t))
					goto default;

				implementation.WriteLine ($"\tif (!__result)");
				implementation.WriteLine ($"\t\t return nil;");
				// TODO: cheating by reusing `initForSuper` - maybe a better name is needed
				implementation.WriteLine ($"\t{GetTypeName (t)}* __peer = [[{GetTypeName (t)} alloc] initForSuper];");
				implementation.WriteLine ("\t__peer->_object = mono_embeddinator_create_object (__result);");
				implementation.WriteLine ("\treturn __peer;");
				break;
			default:
				throw new NotImplementedException ($"Returning type {t.Name} from native code");
			}
		}

		void WriteFile (string name, string content)
		{
			Console.WriteLine ($"\tGenerated: {name}");
			File.WriteAllText (name, content);
		}

		public override void Write (string outputDirectory)
		{
			WriteFile (Path.Combine (outputDirectory, "bindings.h"), headers.ToString ());
			WriteFile (Path.Combine (outputDirectory, "bindings.m"), implementation.ToString ());
		}

		// TODO complete mapping (only with corresponding tests)
		// TODO override with attribute ? e.g. [Obj.Name ("XAMType")]
		public static string GetTypeName (Type t)
		{
			if (t.IsByRef) {
				var et = t.GetElementType ();
				return GetTypeName (et) + (et.IsValueType ? " " : " _Nonnull ") + "* _Nullable";
			}

			if (t.IsEnum)
				return GetObjCName (t);

			switch (Type.GetTypeCode (t)) {
			case TypeCode.Object:
				switch (t.Namespace) {
				case "System":
					switch (t.Name) {
					case "Object":
					case "ValueType":
						return "NSObject";
					case "Void":
						return "void";
					default:
						return GetObjCName (t);
					}
				default:
					return GetObjCName (t);
				}
			case TypeCode.Boolean:
				return "bool";
			case TypeCode.Char:
				return "unsigned short";
			case TypeCode.Double:
				return "double";
			case TypeCode.Single:
				return "float";
			case TypeCode.Byte:
				return "unsigned char";
			case TypeCode.SByte:
				return "signed char";
			case TypeCode.Int16:
				return "short";
			case TypeCode.Int32:
				return "int";
			case TypeCode.Int64:
				return "long long";
			case TypeCode.UInt16:
				return "unsigned short";
			case TypeCode.UInt32:
				return "unsigned int";
			case TypeCode.UInt64:
				return "unsigned long long";
			case TypeCode.String:
				return "NSString *";
			default:
				throw new NotImplementedException ($"Converting type {t.Name} to a native type name");
			}
		}

		public static string GetMonoName (Type t)
		{
			if (t.IsByRef)
				return GetMonoName (t.GetElementType ()) + "&";

			if (t.IsEnum)
				return t.FullName;

			switch (Type.GetTypeCode (t)) {
			case TypeCode.Object:
				switch (t.Namespace) {
				case "System":
					switch (t.Name) {
					case "Void":
						return "void";
					default:
						return "object";
					}
				default:
					return t.FullName;
				}
			case TypeCode.Boolean:
				return "bool";
			case TypeCode.Char:
				return "char";
			case TypeCode.Double:
				return "double";
			case TypeCode.Single:
				return "single";
			case TypeCode.Byte:
				return "byte";
			case TypeCode.SByte:
				return "sbyte";
			case TypeCode.Int16:
				return "int16";
			case TypeCode.Int32:
				return "int";
			case TypeCode.Int64:
				return "long";
			case TypeCode.UInt16:
				return "uint16";
			case TypeCode.UInt32:
				return "uint";
			case TypeCode.UInt64:
				return "ulong";
			case TypeCode.String:
				return "string";
			default:
				throw new NotImplementedException ($"Converting type {t.Name} to a mono type name");
			}
		}

		public static string CamelCase (string s)
		{
			if (s == null)
				return null;
			if (s.Length == 0)
				return String.Empty;
			return Char.ToLowerInvariant (s [0]) + s.Substring (1, s.Length - 1);
		}

		public static string PascalCase (string s)
		{
			if (s == null)
				return null;
			if (s.Length == 0)
				return String.Empty;
			return Char.ToUpperInvariant (s [0]) + s.Substring (1, s.Length - 1);
		}

		public static string SanitizeName (string name)
		{
			StringBuilder sb = null;

			for (int i = 0; i < name.Length; i++) {
				var ch = name [i];
				switch (ch) {
				case '.':
				case '+':
				case '/':
				case '`':
				case '@':
				case '<':
				case '>':
				case '$':
				case '-':
				case ' ':
					if (sb == null)
						sb = new StringBuilder (name, 0, i, name.Length);
					sb.Append ('_');
					break;
				default:
					if (sb != null)
						sb.Append (ch);
					break;
				}
			}

			if (sb != null)
				return sb.ToString ();
			return name;
		}
	}
}
