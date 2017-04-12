﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

using Embeddinator;

namespace ObjC {
	
	public class ObjCGenerator : Generator {

		static TextWriter headers = new StringWriter ();
		static TextWriter implementation = new StringWriter ();

		static ParameterInfo [] NoParameters = new ParameterInfo [0];

		List<Type> types = new List<Type> ();
		Dictionary<Type, List<ConstructorInfo>> ctors = new Dictionary<Type, List<ConstructorInfo>> ();
		Dictionary<Type, List<MethodInfo>> methods = new Dictionary<Type, List<MethodInfo>> ();
		Dictionary<Type, List<PropertyInfo>> properties = new Dictionary<Type, List<PropertyInfo>> ();

		public override void Process (IEnumerable<Assembly> assemblies)
		{
			foreach (var a in assemblies) {
				foreach (var t in a.GetTypes ()) {
					if (!t.IsPublic)
						continue;
					// gather types for forward declarations
					types.Add (t);

					var constructors = new List<ConstructorInfo> ();
					foreach (var ctor in t.GetConstructors ()) {
						// .cctor not to be called directly by native code
						if (ctor.IsStatic)
							continue;
						if (!ctor.IsPublic)
							continue;
						constructors.Add (ctor);
					}
					constructors = constructors.OrderBy ((arg) => arg.ParameterCount).ToList ();
					ctors.Add (t, constructors);

					var meths = new List<MethodInfo> ();
					foreach (var mi in t.GetMethods (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)) {
						meths.Add (mi);
					}
					methods.Add (t, meths);

					var props = new List<PropertyInfo> ();
					foreach (var pi in t.GetProperties (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)) {
						var getter = pi.GetGetMethod ();
						var setter = pi.GetSetMethod ();
						// setter only property are valid in .NET and we need to generate a method in ObjC (there's no writeonly properties)
						if (getter == null)
							continue;
						// we can do better than methods for the more common cases (readonly and readwrite)
						meths.Remove (getter);
						meths.Remove (setter);
						props.Add (pi);
					}
					props = props.OrderBy ((arg) => arg.Name).ToList ();
					properties.Add (t, props);
				}
			}
			types = types.OrderBy ((arg) => arg.FullName).OrderBy ((arg) => types.Contains (arg.BaseType)).ToList ();
			Console.WriteLine ($"\t{types.Count} types found");
		}

		public override void Generate (IEnumerable<Assembly> assemblies)
		{
			headers.WriteLine ("#include \"mono_embeddinator.h\"");
			headers.WriteLine ("#import <Foundation/Foundation.h>");
			headers.WriteLine ();
			headers.WriteLine ();
			headers.WriteLine ("#if !__has_feature(objc_arc)");
			headers.WriteLine ("#error Embeddinator code must be built with ARC.");
			headers.WriteLine ("#endif");
			headers.WriteLine ();
			headers.WriteLine ("MONO_EMBEDDINATOR_BEGIN_DECLS");
			headers.WriteLine ();

			headers.WriteLine ("// forward declarations");
			foreach (var t in types)
				headers.WriteLine ($"@class {GetTypeName (t)};");
			headers.WriteLine ();

			implementation.WriteLine ("#include \"bindings.h\"");
			implementation.WriteLine ("#include \"glib.h\"");
			implementation.WriteLine ("#include \"objc-support.h\"");
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

			headers.WriteLine ();
			headers.WriteLine ("MONO_EMBEDDINATOR_END_DECLS");
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

			foreach (var t in types) {
				Generate (t);
			}
		}

		void GetSignatures (string objName, string monoName, MemberInfo info, ParameterInfo[] parameters, out string objcSignature, out string monoSignature)
		{
			var method = (info as MethodBase); // else it's a PropertyInfo
			// special case for setter-only - the underscore looks ugly
			if ((method != null) && method.IsSpecialName)
				objName = objName.Replace ("_", String.Empty);
			StringBuilder objc = new StringBuilder (objName);
			var mono = new StringBuilder (monoName);
			mono.Append ('(');
			int n = 0;
			foreach (var p in parameters) {
				if (objc.Length > objName.Length) {
					objc.Append (' ');
					mono.Append (',');
				}
				if (method != null) {
					if (n == 0) {
						if (method.IsConstructor || !method.IsSpecialName)
							objc.Append (PascalCase (p.Name));
					} else
						objc.Append (p.Name.ToLowerInvariant ());
				}
				objc.Append (":(").Append (GetTypeName (p.ParameterType)).Append (") ").Append (p.Name);
				mono.Append (GetMonoName (p.ParameterType));
				n++;
			}
			mono.Append (')');

			objcSignature = objc.ToString ();
			monoSignature = mono.ToString ();
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
					var args = "nil";
					if (pcount > 0) {
						Generate (parameters);
						args = "__args";
					}
					implementation.WriteLine ($"\t\tmono_runtime_invoke (__method, __instance, {args}, &__exception);");
					implementation.WriteLine ("\t\tif (__exception)");
					// TODO: Apple often do NSLog (or asserts but they are more brutal) and returning nil is allowed (and common)
					implementation.WriteLine ("\t\t\treturn nil;");
					//implementation.WriteLine ("\t\t\tmono_embeddinator_throw_exception (__exception);");
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

		void Generate (ParameterInfo [] parameters)
		{
			var pcount = parameters.Length;
			implementation.WriteLine ($"\t\tvoid* __args [{pcount}];");
			for (int i = 0; i < pcount; i++) {
				var p = parameters [i];
				switch (Type.GetTypeCode (p.ParameterType)) {
				case TypeCode.String:
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
					implementation.WriteLine ($"\t\t__args [{i}] = &{p.Name};");
					break;
				default:
					throw new NotImplementedException ($"Converting type {p.ParameterType.FullName} to mono code");
				}
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
			else
				headers.Write (", readwrite");
			var property_type = GetTypeName (pi.PropertyType);
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

			var args = "nil";
			if (parametersInfo.Length > 0) {
				Generate (parametersInfo);
				args = "__args";
			}

			implementation.WriteLine ("\tMonoObject* __exception = nil;");
			var instance = "nil";
			if (!info.IsStatic) {
				implementation.WriteLine ($"\tMonoObject* instance = mono_gchandle_get_target (_object->_handle);");
				instance = "instance";
			}

			implementation.Write ("\t");
			if (!IsVoid (info.ReturnType))
				implementation.Write ("MonoObject* __result = ");
			implementation.WriteLine ($"mono_runtime_invoke (__method, {instance}, {args}, &__exception);");

			implementation.WriteLine ("\tif (__exception)");
			implementation.WriteLine ("\t\tmono_embeddinator_throw_exception (__exception);");
			ReturnValue (info.ReturnType);
			implementation.WriteLine ("}");
			implementation.WriteLine ();
		}

		public static bool IsVoid (Type t)
		{
			if (t.Name != "Void")
				return false;
			return (t.Namespace == "System");
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
			switch (Type.GetTypeCode (t)) {
			case TypeCode.Object:
				switch (t.Namespace) {
				case "System":
					switch (t.Name) {
					case "Object":
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
			switch (Type.GetTypeCode (t)) {
			case TypeCode.Object:
				switch (t.Namespace) {
				case "System":
					switch (t.Name) {
					case "Object":
						return "object";
					case "Void":
						return "void";
					default:
						throw new NotImplementedException ($"Converting type {t.Name} to a mono type name");
					}
				default:
					throw new NotImplementedException ($"Converting type {t.Name} to a mono type name");
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

		// get a name that is safe to use from ObjC code
		public static string GetObjCName (Type t)
		{
			return t.FullName.Replace ('.', '_');
		}
	}
}
