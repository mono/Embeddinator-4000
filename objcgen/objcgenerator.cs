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

		static TextWriter headers = new StringWriter ();
		static TextWriter implementation = new StringWriter ();

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

			implementation.WriteLine ("#include \"bindings.h\"");
			implementation.WriteLine ("#include \"glib.h\"");
			implementation.WriteLine ("#include \"objc-support.h\"");
			implementation.WriteLine ("#include \"mono_embeddinator.h\"");
			implementation.WriteLine ("#include <mono/jit/jit.h>");
			implementation.WriteLine ("#include <mono/metadata/assembly.h>");
			implementation.WriteLine ("#include <mono/metadata/object.h>");
			implementation.WriteLine ("#include <mono/metadata/mono-config.h>");
			implementation.WriteLine ("#include <mono/metadata/debug-helpers.h>");
			implementation.WriteLine ();

			implementation.WriteLine ("mono_embeddinator_context_t __mono_context;");
			implementation.WriteLine ();

			foreach (var a in assemblies)
				implementation.WriteLine ($"MonoImage* __{a.GetName ().Name}_image;");
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
		}

		protected override void Generate (Assembly a)
		{
			var name = a.GetName ().Name;
			implementation.WriteLine ($"static void __lookup_assembly_{name} ()");
			implementation.WriteLine ("{");
			implementation.WriteLine ($"\tif (__{name}_image)");
			implementation.WriteLine ("\t\treturn;");
			implementation.WriteLine ("\t__initialize_mono ();");
			implementation.WriteLine ($"\t__{name}_image = mono_embeddinator_load_assembly (&__mono_context, \"{name}.dll\");");
			implementation.WriteLine ($"\tassert (__{name}_image && \"Could not load the assembly '{name}.dll'.\");");
			implementation.WriteLine ("}");
			implementation.WriteLine ();

			foreach (var t in enums) {
				GenerateEnum (t);
			}

			foreach (var t in types) {
				Generate (t);
			}
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
			headers.WriteLine ($"// {t.AssemblyQualifiedName}");
			headers.WriteLine ($"@interface {native_name} : {GetTypeName (t.BaseType)} {{");
			if (!static_type && !has_bound_base_class) {
				headers.WriteLine ("\tMonoEmbedObject* _object;");
			}
			headers.WriteLine ("}");
			headers.WriteLine ();

			implementation.WriteLine ();
			implementation.WriteLine ($"// {t.AssemblyQualifiedName}");
			implementation.WriteLine ($"@implementation {native_name} {{");
			// our internal field is only needed once in the type hierarchy
			implementation.WriteLine ("}");
			implementation.WriteLine ();

			implementation.WriteLine ("+ (void) initialize");
			implementation.WriteLine ("{");
			implementation.WriteLine ($"\tif (self != [{managed_name} class])");
			implementation.WriteLine ("\t\treturn;");
			var aname = t.Assembly.GetName ().Name;
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
					headers.WriteLine ("// List of unavailable initializers. See Constructors v.s. Initializers in our docs");
					headers.WriteLine ("// https://github.com/mono/Embeddinator-4000/blob/master/docs/ObjC.md");
					foreach (var uctor in unavailableCtors) {
						var ctorparams = uctor.GetParameters ();
						string name = "init";
						string signature = ".ctor()";
						if (ctorparams.Length > 0)
							GetSignatures ("initWith", uctor.Name, uctor, ctorparams, out name, out signature);
						headers.WriteLine ($"- (instancetype){name} NS_UNAVAILABLE;");
					}
					headers.WriteLine ();
				}

				foreach (var ctor in constructors) {
					var pcount = ctor.ParameterCount;
					default_init |= pcount == 0;

					var parameters = ctor.GetParameters ();
					string name = "init";
					string signature = ".ctor()";
					if (parameters.Length > 0)
						GetSignatures ("initWith", ctor.Name, ctor, parameters, out name, out signature);

					headers.WriteLine ($"- (instancetype){name};");

					implementation.WriteLine ($"- (instancetype){name}");
					implementation.WriteLine ("{");
					implementation.WriteLine ("\tstatic MonoMethod* __method = nil;");
					implementation.WriteLine ("\tif (!__method) {");
					implementation.WriteLine ("#if TOKENLOOKUP");
					implementation.WriteLine ($"\t\t__method = mono_get_method (__{aname}_image, 0x{ctor.MetadataToken:X8}, {managed_name}_class);");
					implementation.WriteLine ("#else");
					implementation.WriteLine ($"\t\tconst char __method_name [] = \"{t.FullName}:{signature}\";");
					implementation.WriteLine ($"\t\t__method = mono_embeddinator_lookup_method (__method_name, {managed_name}_class);");
					implementation.WriteLine ("#endif");
					implementation.WriteLine ("\t}");
					// TODO: this logic will need to be update for managed NSObject types (e.g. from XI / XM) not to call [super init]
					implementation.WriteLine ("\tif (!_object) {");
					implementation.WriteLine ($"\t\tMonoObject* __instance = mono_object_new (__mono_context.domain, {managed_name}_class);");
					implementation.WriteLine ("\t\tMonoObject* __exception = nil;");
					string postInvoke = String.Empty;
					var args = "nil";
					if (pcount > 0) {
						Generate (parameters, out postInvoke);
						args = "__args";
					}
					var instance = "__instance";
					if (t.IsValueType) {
						implementation.WriteLine ($"\t\tvoid* __unboxed = mono_object_unbox (__instance);");
						instance = "__unboxed";
					}
					implementation.WriteLine ($"\t\tmono_runtime_invoke (__method, {instance}, {args}, &__exception);");
					implementation.WriteLine ("\t\tif (__exception)");
					// TODO: Apple often do NSLog (or asserts but they are more brutal) and returning nil is allowed (and common)
					implementation.WriteLine ("\t\t\treturn nil;");
					//implementation.WriteLine ("\t\t\tmono_embeddinator_throw_exception (__exception);");
					implementation.Write (postInvoke);
					implementation.WriteLine ("\t\t_object = mono_embeddinator_create_object (__instance);");
					implementation.WriteLine ("\t}");
					if (types.Contains (t.BaseType))
						implementation.WriteLine ("\treturn self = [super initForSuper];");
					else
						implementation.WriteLine ("\treturn self = [super init];");
					implementation.WriteLine ("}");
					implementation.WriteLine ();
				}
			}

			if (!default_init || static_type) {
				if (static_type)
					headers.WriteLine ("// a .net static type cannot be initialized");
				headers.WriteLine ("- (instancetype)init NS_UNAVAILABLE;");
				headers.WriteLine ("+ (instancetype)new NS_UNAVAILABLE;");
			}

			// TODO we should re-use the base `init` when it exists
			if (!static_type) {
				headers.WriteLine ("- (instancetype)initForSuper;");

				implementation.WriteLine ("// only when `init` is not generated and we have subclasses");
				implementation.WriteLine ("- (instancetype) initForSuper {");
				// calls super's initForSuper until we reach a non-generated type
				if (types.Contains (t.BaseType))
					implementation.WriteLine ("\treturn self = [super initForSuper];");
				else
					implementation.WriteLine ("\treturn self = [super init];");
				implementation.WriteLine ("}");
				implementation.WriteLine ();
			}

			headers.WriteLine ();
			List<PropertyInfo> props;
			if (properties.TryGetValue (t, out props)) {
				foreach (var pi in props)
					Generate (pi);
				headers.WriteLine ();
			}

			headers.WriteLine ();
			List<MethodInfo> meths;
			if (methods.TryGetValue (t, out meths)) {
				foreach (var mi in meths)
					Generate (mi);
				headers.WriteLine ();
			}

			headers.WriteLine ("@end");
			headers.WriteLine ();

			implementation.WriteLine ("@end");
			implementation.WriteLine ();
		}

		void Generate (ParameterInfo [] parameters, out string postInvoke)
		{
			StringBuilder post = new StringBuilder ();
			var pcount = parameters.Length;
			implementation.WriteLine ($"\t\tvoid* __args [{pcount}];");
			for (int i = 0; i < pcount; i++) {
				var p = parameters [i];

				var pt = p.ParameterType;
				var is_by_ref = pt.IsByRef;
				if (is_by_ref)
					pt = pt.GetElementType ();

				switch (Type.GetTypeCode (pt)) {
				case TypeCode.String:
					if (is_by_ref) {
						implementation.WriteLine ($"\t\tMonoString* __string = *{p.Name} ? mono_string_new (__mono_context.domain, [*{p.Name} UTF8String]) : nil;");
						implementation.WriteLine ($"\t\t__args [{i}] = &__string;");
						post.AppendLine ($"\t\t*{p.Name} = mono_embeddinator_get_nsstring (__string);");
					} else
						implementation.WriteLine ($"\t\t__args [{i}] = {p.Name} ? mono_string_new (__mono_context.domain, [{p.Name} UTF8String]) : nil;");
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
						implementation.WriteLine ($"\t\t__args [{i}] = {p.Name};");
					else
						implementation.WriteLine ($"\t\t__args [{i}] = &{p.Name};");
					break;
				default:
					if (pt.IsValueType)
						implementation.WriteLine ($"\t\t__args [{i}] = mono_object_unbox (mono_gchandle_get_target ({p.Name}->_object->_handle));");
					else if (types.Contains (pt))
						implementation.WriteLine ($"\t\t__args [{i}] = {p.Name};");
					else
						throw new NotImplementedException ($"Converting type {pt.FullName} to mono code");
					break;
				}
			}
			postInvoke = post.ToString ();
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
			else
				headers.Write (", readwrite");
			var pt = pi.PropertyType;
			var property_type = GetTypeName (pt);
			if (types.Contains (pt))
				property_type += " *";
			headers.WriteLine ($") {property_type} {name};");

			ImplementMethod (getter, name, pi);
			if (setter == null)
				return;

			ImplementMethod (setter, "set" + pi.Name, pi);
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
		void ImplementMethod (MethodInfo info, string name, PropertyInfo pi = null)
		{
			var type = info.DeclaringType;
			var managed_type_name = GetObjCName (type);
			var managed_name = info.Name;
			var return_type = GetReturnType (type, info.ReturnType);
			var parametersInfo = info.GetParameters ();

			string objcsig;
			string monosig;
			GetSignatures (name, managed_name, (MemberInfo) pi ?? info, parametersInfo, out objcsig, out monosig);

			if (pi == null) {
				headers.Write (info.IsStatic ? '+' : '-');
				headers.WriteLine ($" ({return_type}){objcsig};");
			}

			implementation.Write (info.IsStatic ? '+' : '-');
			implementation.WriteLine ($" ({return_type}) {objcsig}");
			implementation.WriteLine ("{");
			implementation.WriteLine ("\tstatic MonoMethod* __method = nil;");
			implementation.WriteLine ("\tif (!__method) {");
			implementation.WriteLine ("#if TOKENLOOKUP");
			var aname = type.Assembly.GetName ().Name;
			implementation.WriteLine ($"\t\t__method = mono_get_method (__{aname}_image, 0x{info.MetadataToken:X8}, {managed_type_name}_class);");
			implementation.WriteLine ("#else");
			implementation.WriteLine ($"\t\tconst char __method_name [] = \"{type.FullName}:{monosig})\";");
			implementation.WriteLine ($"\t\t__method = mono_embeddinator_lookup_method (__method_name, {managed_type_name}_class);");
			implementation.WriteLine ("#endif");
			implementation.WriteLine ("\t}");

			string postInvoke = String.Empty;
			var args = "nil";
			if (parametersInfo.Length > 0) {
				Generate (parametersInfo, out postInvoke);
				args = "__args";
			}

			implementation.WriteLine ("\tMonoObject* __exception = nil;");
			var instance = "nil";
			if (!info.IsStatic) {
				implementation.WriteLine ($"\tMonoObject* __instance = mono_gchandle_get_target (_object->_handle);");
				if (type.IsValueType) {
					implementation.WriteLine ($"\t\tvoid* __unboxed = mono_object_unbox (__instance);");
					instance = "__unboxed";
				} else {
					instance = "__instance";
				}
			}

			implementation.Write ("\t");
			if (!info.ReturnType.Is ("System", "Void"))
				implementation.Write ("MonoObject* __result = ");
			implementation.WriteLine ($"mono_runtime_invoke (__method, {instance}, {args}, &__exception);");

			implementation.WriteLine ("\tif (__exception)");
			implementation.WriteLine ("\t\tmono_embeddinator_throw_exception (__exception);");
			// ref and out parameters might need to be converted back
			implementation.Write (postInvoke);
			ReturnValue (info.ReturnType);
			implementation.WriteLine ("}");
			implementation.WriteLine ();
		}

		protected override void Generate (MethodInfo mi)
		{
			var name = CamelCase (mi.Name);
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
			if (t.IsByRef)
				return GetTypeName (t.GetElementType ()) + "*";

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
					return t.IsValueType ? t.FullName : "object";
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
	}
}
