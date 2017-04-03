using System;
using System.Collections.Generic;
using System.IO;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

using Embeddinator;

namespace ObjC {
	
	public class ObjCGenerator : Generator {

		static TextWriter headers = new StringWriter ();
		static TextWriter implementation = new StringWriter ();

		List<Type> types = new List<Type> ();

		public override void Process (IEnumerable<Assembly> assemblies)
		{
			foreach (var a in assemblies) {
				foreach (var t in a.GetTypes ()) {
					if (!t.IsPublic)
						continue;
					// gather types for forward declarations
					types.Add (t);
				}
			}
			Console.WriteLine ($"\t{types.Count} types found");
		}

		public override void Generate (IEnumerable<Assembly> assemblies)
		{
			headers.WriteLine ("#include \"mono_embeddinator.h\"");
			headers.WriteLine ("#import <Foundation/Foundation.h>");
			headers.WriteLine ();
			headers.WriteLine ("MONO_EMBEDDINATOR_BEGIN_DECLS");
			headers.WriteLine ();

			// TODO: sort ? so it's easier to check if a type is present in a long list
			headers.WriteLine ("// forward declarations");
			foreach (var t in types)
				headers.WriteLine ($"@class {GetTypeName (t)};");
			headers.WriteLine ();

			implementation.WriteLine ("#include \"bindings.h\"");
			implementation.WriteLine ("#include \"glib.h\"");
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
				implementation.WriteLine ($"static MonoClass* {t.Name}_class = nil;");
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
			implementation.WriteLine ($"\t__{name}_image = mono_embeddinator_load_assembly (&__mono_context, \"{name}.dll\");");
			implementation.WriteLine ("}");
			implementation.WriteLine ();

			foreach (var t in a.GetTypes ()) {
				if (!t.IsPublic)
					continue;
				Generate (t);
			}
		}

		protected override void Generate (Type t)
		{
			var managed_name = t.Name;
			implementation.WriteLine ($"static void __lookup_class_{managed_name} ()");
			implementation.WriteLine ("{");
			implementation.WriteLine ($"\tif (!{managed_name}_class) {{");
			implementation.WriteLine ("\t\t__initialize_mono ();");
			implementation.WriteLine ("\t\t__lookup_assembly_managed ();");
			implementation.WriteLine ($"\t\t{managed_name}_class = mono_class_from_name (__{t.Assembly.GetName ().Name}_image, \"{t.Namespace}\", \"{managed_name}\");");
			implementation.WriteLine ("\t}");
			implementation.WriteLine ("}");
			implementation.WriteLine ();

			var native_name = GetTypeName (t);
			headers.WriteLine ($"// {t.AssemblyQualifiedName}");
			headers.WriteLine ($"@interface {native_name} : {GetTypeName (t.BaseType)} {{");
			headers.WriteLine ("\tMonoEmbedObject* _object;");
			headers.WriteLine ("}");
			headers.WriteLine ();

			implementation.WriteLine ($"// {t.AssemblyQualifiedName}");
			implementation.WriteLine ($"@implementation {native_name}");
			implementation.WriteLine ();

			var default_init = false;
			foreach (var ctor in t.GetConstructors ()) {
				// .cctor not to be called directly by native code
				if (ctor.IsStatic)
					continue;
				if (!ctor.IsPublic)
					continue;
				if (ctor.ParameterCount == 0) {
					headers.WriteLine ("- (instancetype)init;");

					implementation.WriteLine ("- (instancetype)init");
					implementation.WriteLine ("{");
					implementation.WriteLine ($"\tconst char __method_name [] = \"{t.FullName}:.ctor()\";");
					implementation.WriteLine ("\tstatic MonoMethod* __method = nil;");
					implementation.WriteLine ("\tif (!__method) {");
					implementation.WriteLine ($"\t\t__lookup_class_{managed_name} ();");
					implementation.WriteLine ($"\t\t__method = mono_embeddinator_lookup_method (__method_name, {managed_name}_class);");
					implementation.WriteLine ("\t}");
					// TODO: this logic will need to be update for managed NSObject types (e.g. from XI / XM) not to call [super init]
					implementation.WriteLine ("\tif (self = [super init]) {");
					implementation.WriteLine ($"\t\tMonoObject* __instance = mono_object_new (__mono_context.domain, {managed_name}_class);");
					implementation.WriteLine ("\t\tMonoObject* __exception = nil;");
					implementation.WriteLine ("\t\tmono_runtime_invoke (__method, __instance, nil, &__exception);");
					implementation.WriteLine ("\t\tif (__exception)");
					// TODO: Apple often do NSLog (or asserts but they are more brutal) and returning nil is allowed (and common)
					implementation.WriteLine ("\t\t\treturn nil;");
					//implementation.WriteLine ("\t\t\tmono_embeddinator_throw_exception (__exception);");
					implementation.WriteLine ("\t\t_object = mono_embeddinator_create_object (__instance);");
					implementation.WriteLine ("\t\t_object->_handle = mono_gchandle_new (__instance, /*pinned=*/false);");
					implementation.WriteLine ("\t}");
					implementation.WriteLine ("\treturn self;");
					implementation.WriteLine ("}");
					implementation.WriteLine ();
					default_init = true;
				}
			}

			var static_type = t.IsSealed && t.IsAbstract;
			if (!default_init || static_type) {
				headers.WriteLine ();
				if (static_type)
					headers.WriteLine ("// a .net static type cannot be initialized");
				headers.WriteLine ("- (instancetype)init NS_UNAVAILABLE;");
			}

			headers.WriteLine ();
			foreach (var pi in t.GetProperties ())
				Generate (pi);

			headers.WriteLine ("@end");
			headers.WriteLine ();

			implementation.WriteLine ("@end");
			implementation.WriteLine ();
		}

		protected override void Generate (PropertyInfo pi)
		{
			var getter = pi.GetGetMethod ();
			var setter = pi.GetSetMethod ();
			// FIXME: setter only is valid, even if discouraged, in .NET - we should create a SetX method
			if (getter == null && setter != null)
				throw new NotImplementedException ("Write-only properties");

			// TODO override with attribute ? e.g. [ObjC.Selector ("foo")]
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

			var managed_type_name = pi.DeclaringType.Name;
			implementation.Write (getter.IsStatic ? '+' : '-');
			implementation.WriteLine ($" ({property_type}) {name}");
			implementation.WriteLine ("{");
			implementation.WriteLine ($"\tconst char __method_name [] = \"{managed_type_name}:{getter.Name}()\";");
			implementation.WriteLine ("\tstatic MonoMethod* __method = nil;");
			implementation.WriteLine ("\tif (!__method) {");
			implementation.WriteLine ($"\t\t__lookup_class_{managed_type_name} ();");
			implementation.WriteLine ($"\t\t__method = mono_embeddinator_lookup_method (__method_name, {managed_type_name}_class);");
			implementation.WriteLine ("\t}");
			implementation.WriteLine ("\tMonoObject* __exception = nil;");
			var instance = "nil";
			if (!getter.IsStatic) {
				implementation.WriteLine ($"\t\tMonoObject* instance = mono_gchandle_get_target (_object->_handle);");
				instance = "instance";
			}
			implementation.WriteLine ($"\tMonoObject* __result = mono_runtime_invoke (__method, {instance}, nil, &__exception);");
			implementation.WriteLine ("\tif (__exception)");
			implementation.WriteLine ("\t\tmono_embeddinator_throw_exception (__exception);");
			ReturnValue (pi.PropertyType);
			implementation.WriteLine ("}");
			implementation.WriteLine ();
			if (setter == null)
				return;
			
			// TODO override with attribute ? e.g. [ObjC.Selector ("foo")]
			implementation.Write (getter.IsStatic ? '+' : '-');
			implementation.WriteLine ($" (void) set{pi.Name}:({property_type})value");
			implementation.WriteLine ("{");
			implementation.WriteLine ($"\tconst char __method_name [] = \"{managed_type_name}:{setter.Name}({property_type})\";");
			implementation.WriteLine ("\tstatic MonoMethod* __method = nil;");
			implementation.WriteLine ("\tif (!__method) {");
			implementation.WriteLine ($"\t\t__lookup_class_{managed_type_name} ();");
			implementation.WriteLine ($"\t\t__method = mono_embeddinator_lookup_method (__method_name, {managed_type_name}_class);");
			implementation.WriteLine ("\t}");
			implementation.WriteLine ("\tvoid* __args [1];");
			implementation.WriteLine ("\t__args [0] = &value;");
			implementation.WriteLine ("\tMonoObject* __exception = nil;");
			instance = "nil";
			if (!getter.IsStatic) {
				implementation.WriteLine ($"\t\tMonoObject* instance = mono_gchandle_get_target (_object->_handle);");
				instance = "instance";
			}
			implementation.WriteLine ($"\tmono_runtime_invoke (__method, {instance}, __args, &__exception);");
			implementation.WriteLine ("\tif (__exception)");
			implementation.WriteLine ("\t\tmono_embeddinator_throw_exception (__exception);");
			implementation.WriteLine ("}");
			implementation.WriteLine ();
		}

		void ReturnValue (Type t)
		{
			switch (Type.GetTypeCode (t)) {
			// unboxing
			case TypeCode.String:
				implementation.WriteLine ("\tif (__result == NULL)");
				implementation.WriteLine ("\t\treturn NULL;");
				implementation.WriteLine ("\tint length = mono_string_length ((MonoString *) __result);");
				implementation.WriteLine ("\tgunichar2 *str = mono_string_chars ((MonoString *) __result);");
				implementation.WriteLine ("\treturn [[[NSString alloc] initWithBytes: str length: length * 2 encoding: NSUTF16LittleEndianStringEncoding] autorelease];");
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
				return (t.Namespace == "System" && t.Name == "Object") ? "NSObject" : t.FullName.Replace ('.', '_');
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
				return "NSString*";
			default:
				throw new NotImplementedException ($"Converting type {t.Name} to a native type name");
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
	}
}
