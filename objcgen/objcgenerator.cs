using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

using Embeddinator;

namespace ObjC {
	
	public partial class ObjCGenerator : Generator {

		SourceWriter headers = new SourceWriter ();
		SourceWriter implementation = new SourceWriter ();

		public override void Generate ()
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
			foreach (var t in types.Where ((arg) => arg.IsClass))
				headers.WriteLine ($"@class {t.TypeName};");
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
				implementation.WriteLine ($"MonoImage* __{a.SafeName}_image;");
			implementation.WriteLine ();

			foreach (var t in types.Where ((arg) => arg.IsClass))
				implementation.WriteLine ($"static MonoClass* {t.ObjCName}_class = nil;");
			foreach (var t in types.Where ((arg) => arg.IsProtocol)) {
				var pname = t.TypeName;
				headers.WriteLine ($"@protocol {pname};");
				implementation.WriteLine ($"@class __{pname}Wrapper;");
			}
			implementation.WriteLine ();

			implementation.WriteLine ("static void __initialize_mono ()");
			implementation.WriteLine ("{");
			implementation.Indent++;
			implementation.WriteLine ("if (__mono_context.domain)");
			implementation.Indent++;
			implementation.WriteLine ("return;");
			implementation.Indent--;
			implementation.WriteLine ("mono_embeddinator_init (&__mono_context, \"mono_embeddinator_binding\");");
			implementation.Indent--;
			implementation.WriteLine ("}");
			implementation.WriteLine ();

			// The generated registrar code calls this method on every entry point.
			implementation.WriteLine ("void xamarin_embeddinator_initialize ()");
			implementation.WriteLine ("{");
			implementation.WriteLine ("\t__initialize_mono ();");
			implementation.WriteLine ("}");
			implementation.WriteLine ();

			base.Generate ();

			headers.WriteLine ("NS_ASSUME_NONNULL_END");
			headers.WriteLine ();
		}

		protected override void Generate (ProcessedAssembly a)
		{
			var originalName = a.Name;
			var name = a.SafeName;
			implementation.WriteLine ($"static void __lookup_assembly_{name} ()");
			implementation.WriteLine ("{");
			implementation.Indent++;
			implementation.WriteLine ($"if (__{name}_image)");
			implementation.Indent++;
			implementation.WriteLine ("return;");
			implementation.Indent--;
			implementation.WriteLine ("__initialize_mono ();");
			implementation.WriteLine ($"__{name}_image = mono_embeddinator_load_assembly (&__mono_context, \"{originalName}.dll\");");
			implementation.WriteLine ($"assert (__{name}_image && \"Could not load the assembly '{originalName}.dll'.\");");
			var categories = extensions_methods.Keys;
			if (categories.Count > 0) {
				implementation.WriteLine ("// we cannot use `+initialize` inside categories as they would replace the original type code");
				implementation.WriteLine ("// since there should not be tons of them we're pre-loading them when loading the assembly");
				foreach (var definedType in extensions_methods.Keys) {
					var managed_name = NameGenerator.GetObjCName (definedType);
					implementation.WriteLineUnindented ("#if TOKENLOOKUP");
					implementation.WriteLine ($"{managed_name}_class = mono_class_get (__{name}_image, 0x{definedType.MetadataToken:X8});");
					implementation.WriteLineUnindented ("#else");
					implementation.WriteLine ($"{managed_name}_class = mono_class_from_name (__{name}_image, \"{definedType.Namespace}\", \"{definedType.Name}\");");
					implementation.WriteLineUnindented ("#endif");
				}
			}
			implementation.Indent--;
			implementation.WriteLine ("}");
			implementation.WriteLine ();

			var assembly = a.Assembly;

			foreach (var t in types.Where ((arg) => arg.IsEnum && arg.Assembly == a)) {
				GenerateEnum (t);
			}

			foreach (var t in types.Where ((arg) => arg.IsProtocol && arg.Assembly == a)) {
				GenerateProtocol (t);
			}

			foreach (var t in types.Where ((arg) => arg.IsClass && arg.Assembly == a)) {
				Generate (t);
			}

			foreach (var extension in extensions_methods) {
				var defining_type = extension.Key;
				if (defining_type.Assembly != assembly)
					continue;
				foreach (var category in extension.Value)
					GenerateCategory (defining_type, category.Key, category.Value);
			}
		}

		void GenerateCategory (Type definedType, Type extendedType, List<MethodInfo> methods)
		{
			var etn = NameGenerator.GetTypeName (extendedType).Replace (" *", String.Empty);
			var name = $"{etn} ({NameGenerator.GetTypeName (definedType)})";
			headers.WriteLine ($"/** Category {name}");
			headers.WriteLine ($" *  Corresponding .NET Qualified Name: `{definedType.AssemblyQualifiedName}`");
			headers.WriteLine (" */");
			headers.WriteLine ($"@interface {name}");
			headers.WriteLine ();

			implementation.WriteLine ($"@implementation {name}");
			implementation.WriteLine ();

			foreach (var mi in methods) {
				ImplementMethod (mi, mi.Name.CamelCase (), true);
			}

			headers.WriteLine ("@end");
			headers.WriteLine ();

			implementation.WriteLine ("@end");
			implementation.WriteLine ();
		}

		void GenerateEnum (ProcessedType type)
		{
			Type t = type.Type;
			var managed_name = type.ObjCName;
			var underlying_type = t.GetEnumUnderlyingType ();
			var base_type = NameGenerator.GetTypeName (underlying_type);

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
			headers.Indent++;
			foreach (var name in t.GetEnumNames ()) {
				var value = t.GetField (name).GetRawConstantValue ();
				headers.Write ($"{managed_name}{name} = ");
				if (flags)
					headers.Write ($"0x{value:x}");
				else
					headers.Write (value);
				headers.WriteLine (',');
			}
			headers.Indent--;
			headers.WriteLine ("};");
			headers.WriteLine ();
		}

		void GenerateProtocol (ProcessedType type)
		{
			Type t = type.Type;
			var pbuilder = new ProtocolHelper (headers, implementation) {
				AssemblyQualifiedName = t.AssemblyQualifiedName,
				AssemblyName = t.Assembly.GetName ().Name.Sanitize (),
				ProtocolName = NameGenerator.GetTypeName (t),
				Namespace = t.Namespace,
				ManagedName = t.Name,
				MetadataToken = t.MetadataToken,
			};
			pbuilder.BeginHeaders ();

			// no need to iterate constructors or fields as they cannot be part of net interfaces
			// do not generate implementations for protocols
			implementation.Enabled = false;

			List<ProcessedProperty> props;
			if (properties.TryGetValue (t, out props)) {
				headers.WriteLine ();
				foreach (var pi in props)
					Generate (pi);
			}

			List<ProcessedMethod> meths;
			if (methods.TryGetValue (t, out meths)) {
				headers.WriteLine ();
				foreach (var mi in meths)
					Generate (mi);
			}

			pbuilder.EndHeaders ();

			// wrappers are internal so not part of the headers
			headers.Enabled = false;
			implementation.Enabled = true;

			pbuilder.BeginImplementation ();

			if (properties.TryGetValue (t, out props)) {
				implementation.WriteLine ();
				foreach (var pi in props)
					Generate (pi);
			}

			if (methods.TryGetValue (t, out meths)) {
				implementation.WriteLine ();
				foreach (var mi in meths)
					Generate (mi);
			}

			pbuilder.EndImplementation ();
			headers.Enabled = true;
		}

		protected override void Generate (ProcessedType type)
		{
			Type t = type.Type;
			var aname = t.Assembly.GetName ().Name.Sanitize ();
			var static_type = t.IsSealed && t.IsAbstract;

			var managed_name = NameGenerator.GetObjCName (t);

			List<string> conformed_protocols = new List<string> ();
			foreach (var i in t.GetInterfaces ()) {
				if (types.HasProtocol (i))
					conformed_protocols.Add (NameGenerator.GetObjCName (i));
			}

			var tbuilder = new ClassHelper (headers, implementation) {
				AssemblyQualifiedName = t.AssemblyQualifiedName,
				AssemblyName = aname,
				BaseTypeName = NameGenerator.GetTypeName (t.BaseType),
				Name = NameGenerator.GetTypeName (t),
				Namespace = t.Namespace,
				ManagedName = t.Name,
				Protocols = conformed_protocols,
				IsBaseTypeBound = types.HasClass (t.BaseType),
				IsStatic = t.IsSealed && t.IsAbstract,
				MetadataToken = t.MetadataToken,
			};

			tbuilder.BeginHeaders ();
			tbuilder.BeginImplementation ();

			var default_init = false;
			List<ProcessedConstructor> constructors;
			if (ctors.TryGetValue (t, out constructors)) {
				// First get the unavailable init ctor selectors in parent class
				var unavailableCtors = GetUnavailableParentCtors (t, constructors);
				if (unavailableCtors.Count () > 0) {
					// TODO: Print a #pragma mark once we have a well defined header structure http://nshipster.com/pragma/
					foreach (var uctor in unavailableCtors) {
						var ctorparams = uctor.Constructor.GetParameters ();
						string name = "init";
						string signature = ".ctor()";
						if (ctorparams.Length > 0)
							GetSignatures ("initWith", uctor.Constructor.Name, uctor.Constructor, ctorparams, uctor.FallBackToTypeName, false, out name, out signature);
						headers.WriteLine ("/** This initializer is not available as it was not re-exposed from the base type");
						headers.WriteLine (" *  For more details consult https://github.com/mono/Embeddinator-4000/blob/master/docs/ObjC.md#constructors-vs-initializers");
						headers.WriteLine (" */");
						headers.WriteLine ($"- (nullable instancetype){name} NS_UNAVAILABLE;");
						headers.WriteLine ();
					}
				}

				foreach (var ctor in constructors) {
					var pcount = ctor.Constructor.ParameterCount;
					default_init |= pcount == 0;

					var parameters = ctor.Constructor.GetParameters ();
					string name = "init";
					string signature = ".ctor()";
					if (parameters.Length > 0)
						GetSignatures ("initWith", ctor.Constructor.Name, ctor.Constructor, parameters, ctor.FallBackToTypeName, false, out name, out signature);

					var builder = new MethodHelper (headers, implementation) {
						AssemblySafeName = aname,
						ReturnType = "nullable instancetype",
						ManagedTypeName = t.FullName,
						MetadataToken = ctor.Constructor.MetadataToken,
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
					implementation.WriteLine ("if (!_object) {");
					implementation.Indent++;
					implementation.WriteLine ($"MonoObject* __instance = mono_object_new (__mono_context.domain, {managed_name}_class);");

					string postInvoke = String.Empty;
					var args = "nil";
					if (pcount > 0) {
						Generate (parameters, false, out postInvoke);
						args = "__args";
					}
					builder.WriteInvoke (args);
					implementation.Write (postInvoke);
					implementation.WriteLine ("_object = mono_embeddinator_create_object (__instance);");
					implementation.Indent--;
					implementation.WriteLine ("}");
					if (types.HasClass (t.BaseType))
						implementation.WriteLine ("return self = [super initForSuper];");
					else
						implementation.WriteLine ("return self = [super init];");
					builder.EndImplementation ();

					headers.WriteLine ();

					if (members_with_default_values.Contains (ctor.Constructor))
						default_init |= GenerateDefaultValuesWrappers (name, ctor.Constructor);
				}
			}

			// generate an `init` for a value type (even if none was defined, the default one is usable)
			if (!default_init && t.IsValueType) {
				var builder = new MethodHelper (headers, implementation) {
					AssemblySafeName = aname,
					ReturnType = "nullable instancetype",
					ManagedTypeName = t.FullName,
					MonoSignature = ".ctor()",
					ObjCSignature = "init",
					ObjCTypeName = managed_name,
					IsConstructor = true,
					IsValueType = t.IsValueType,
					IgnoreException = true,
				};

				builder.WriteHeaders ();
				builder.BeginImplementation ();
				// no call to `WriteMethodLookup` since there is not such method if we reached this case

				implementation.WriteLine ("if (!_object) {");
				implementation.Indent++;
				implementation.WriteLine ($"MonoObject* __instance = mono_object_new (__mono_context.domain, {managed_name}_class);");
				// no call to `WriteInvoke` since there is not such method if we reached this case
				implementation.WriteLine ("_object = mono_embeddinator_create_object (__instance);");
				implementation.Indent--;
				implementation.WriteLine ("}");
				if (types.HasClass (t.BaseType))
					implementation.WriteLine ("return self = [super initForSuper];");
				else
					implementation.WriteLine ("return self = [super init];");
				builder.EndImplementation ();

				headers.WriteLine ();
				default_init = true;
			}

			if (!default_init || static_type)
				tbuilder.DefineNoDefaultInit ();
	
			List<ProcessedProperty> props;
			if (properties.TryGetValue (t, out props)) {
				headers.WriteLine ();
				foreach (var pi in props)
					Generate (pi);
			}

			List<ProcessedFieldInfo> f;
			if (fields.TryGetValue (t, out f)) {
				headers.WriteLine ();
				foreach (var fi in f)
					Generate (fi);
			}

			List<ProcessedProperty> s;
			if (subscriptProperties.TryGetValue (t, out s)) {
				headers.WriteLine ();
				foreach (var si in s)
					GenerateSubscript (si);
			}

			List<ProcessedMethod> meths;
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
					AssemblySafeName = aname,
					MetadataToken = m.MetadataToken,
					ObjCTypeName = managed_name,
					ManagedTypeName = t.FullName,
					MonoSignature = $"CompareTo({NameGenerator.GetMonoName (pt)})",
				};
				builder.WriteHeaders ();
				builder.WriteImplementation ();
			}

			if (equals.TryGetValue (t, out m)) {
				var builder = new EqualsHelper (headers, implementation) {
					AssemblySafeName = aname,
					MetadataToken = m.MetadataToken,
					ObjCTypeName = managed_name,
					ManagedTypeName = t.FullName,
				};

				builder.WriteHeaders ();
				builder.WriteImplementation ();
			}

			if (hashes.TryGetValue (t, out m)) {
				var builder = new HashHelper (headers, implementation) {
					AssemblySafeName = aname,
					MetadataToken = m.MetadataToken,
					ObjCTypeName = managed_name,
					ManagedTypeName = t.FullName,
				};

				builder.WriteHeaders ();
				builder.WriteImplementation ();
			}

			tbuilder.EndHeaders ();
			tbuilder.EndImplementation ();
		}

		void Generate (ParameterInfo [] parameters, bool isExtension, out string postInvoke)
		{
			StringBuilder post = new StringBuilder ();
			var pcount = parameters.Length;
			implementation.WriteLine ($"void* __args [{pcount}];");
			for (int i = 0; i < pcount; i++) {
				var p = parameters [i];
				var name = (isExtension && (i == 0)) ? "self" : NameGenerator.GetExtendedParameterName (p, parameters);
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
					implementation.WriteLine ($"MonoString* __string = *{paramaterName} ? mono_string_new (__mono_context.domain, [*{paramaterName} UTF8String]) : nil;");
					implementation.WriteLine ($"{argumentName} = &__string;");
					post.AppendLine ($"*{paramaterName} = mono_embeddinator_get_nsstring (__string);");
				} else
					implementation.WriteLine ($"{argumentName} = {paramaterName} ? mono_string_new (__mono_context.domain, [{paramaterName} UTF8String]) : nil;");
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
					implementation.WriteLine ($"{argumentName} = {paramaterName};");
				else
					implementation.WriteLine ($"{argumentName} = &{paramaterName};");
				break;
			default:
				if (t.IsValueType)
					implementation.WriteLine ($"{argumentName} = mono_object_unbox (mono_gchandle_get_target ({paramaterName}->_object->_handle));");
				else 
				if (types.HasClass (t))
					implementation.WriteLine ($"{argumentName} = {paramaterName} ? mono_gchandle_get_target ({paramaterName}->_object->_handle): nil;");
				else if (types.HasProtocol (t))
					implementation.WriteLine ($"{argumentName} = {paramaterName} ? mono_embeddinator_get_object ({paramaterName}, true) : nil;");
				else
					throw new NotImplementedException ($"Converting type {t.FullName} to mono code");
				break;
			}
		}

		protected override void Generate (ProcessedProperty property)
		{
			PropertyInfo pi = property.Property;
			var getter = pi.GetGetMethod ();
			var setter = pi.GetSetMethod ();
			// setter-only properties are handled as methods (and should not reach this code)
			if (getter == null && setter != null)
				throw new EmbeddinatorException (99, "Internal error `setter only`. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues");

			var name = pi.Name.CamelCase ();

			headers.Write ("@property (nonatomic");
			if (getter.IsStatic)
				headers.Write (", class");
			if (setter == null)
				headers.Write (", readonly");
			var pt = pi.PropertyType;
			var property_type = NameGenerator.GetTypeName (pt);
			if (types.HasClass (pt))
				property_type += " *";

			var spacing = property_type [property_type.Length - 1] == '*' ? string.Empty : " ";
			headers.WriteLine ($") {property_type}{spacing}{name};");

			ImplementMethod (getter, name, false, pi);
			if (setter == null)
				return;

			ImplementMethod (setter, "set" + pi.Name, false, pi);
		}

		protected void Generate (ProcessedFieldInfo field)
		{
			FieldInfo fi = field.Field;
			bool read_only = fi.IsInitOnly || fi.IsLiteral;

			headers.Write ("@property (nonatomic");
			if (fi.IsStatic)
				headers.Write (", class");
			if (read_only)
				headers.Write (", readonly");
			var ft = fi.FieldType;
			var bound = types.HasClass (ft);
			if (bound && ft.IsValueType)
				headers.Write (", nonnull");

			var field_type = NameGenerator.GetTypeName (ft);
			if (bound)
				field_type += " *";

			var name = fi.Name.CamelCase ();

			var spacing = field_type [field_type.Length - 1] == '*' ? string.Empty : " ";
			headers.WriteLine ($") {field_type}{spacing}{name};");

			// it's similar, but different from implementing a method

			var type = fi.DeclaringType;
			var managed_type_name = NameGenerator.GetObjCName (type);
			var return_type = GetReturnType (type, fi.FieldType);

			implementation.Write (fi.IsStatic ? '+' : '-');
			implementation.WriteLine ($" ({return_type}) {name}");
			implementation.WriteLine ("{");
			implementation.Indent++;
			implementation.WriteLine ("static MonoClassField* __field = nil;");
			implementation.WriteLine ("if (!__field) {");
			implementation.Indent++;
			implementation.WriteLineUnindented ("#if TOKENLOOKUP");
			implementation.WriteLine ($"__field = mono_class_get_field ({managed_type_name}_class, 0x{fi.MetadataToken:X8});");
			implementation.WriteLineUnindented ("#else");
			implementation.WriteLine ($"const char __field_name [] = \"{fi.Name}\";");
			implementation.WriteLine ($"__field = mono_class_get_field_from_name ({managed_type_name}_class, __field_name);");
			implementation.WriteLineUnindented ("#endif");
			implementation.Indent--;
			implementation.WriteLine ("}");
			var instance = "nil";
			if (!fi.IsStatic) {
				implementation.WriteLine ($"MonoObject* __instance = mono_gchandle_get_target (_object->_handle);");
				instance = "__instance";
			}
			implementation.WriteLine ($"MonoObject* __result = mono_field_get_value_object (__mono_context.domain, __field, {instance});");
			if (types.HasClass (ft)) {
				implementation.WriteLine ("if (!__result)");
				implementation.Indent++;
				implementation.WriteLine ("return nil;");
				implementation.Indent--;
			}
			ReturnValue (fi.FieldType);
			implementation.Indent--;
			implementation.WriteLine ("}");
			implementation.WriteLine ();

			if (read_only)
				return;
			implementation.Write (fi.IsStatic ? '+' : '-');
			implementation.WriteLine ($" (void) set{fi.Name}:({field_type})value");
			implementation.WriteLine ("{");
			implementation.Indent++;
			implementation.WriteLine ("static MonoClassField* __field = nil;");
			implementation.WriteLine ("if (!__field) {");
			implementation.Indent++;
			implementation.WriteLineUnindented ("#if TOKENLOOKUP");
			implementation.WriteLine ($"__field = mono_class_get_field ({managed_type_name}_class, 0x{fi.MetadataToken:X8});");
			implementation.WriteLineUnindented ("#else");
			implementation.WriteLine ($"const char __field_name [] = \"{fi.Name}\";");
			implementation.WriteLine ($"__field = mono_class_get_field_from_name ({managed_type_name}_class, __field_name);");
			implementation.WriteLineUnindented ("#endif");
			implementation.Indent--;
			implementation.WriteLine ("}");
			StringBuilder sb = null;
			implementation.WriteLine ($"void* __value;");
			GenerateArgument ("value", "__value", fi.FieldType, ref sb);
			if (fi.IsStatic) {
				implementation.WriteLine ($"MonoVTable *__vtable = mono_class_vtable (__mono_context.domain, {managed_type_name}_class);");
				implementation.WriteLine ("mono_field_static_set_value (__vtable, __field, __value);");
			} else {
				implementation.WriteLine ($"MonoObject* __instance = mono_gchandle_get_target (_object->_handle);");
				implementation.WriteLine ("mono_field_set_value (__instance, __field, __value);");
			}
			implementation.Indent--;
			implementation.WriteLine ("}");
			implementation.WriteLine ();
		}

		public string GetReturnType (Type declaringType, Type returnType)
		{
			if (types.HasProtocol (returnType))
				return "id<" + NameGenerator.GetTypeName (returnType) + ">";
			if (declaringType == returnType)
				return "instancetype";

			var return_type = NameGenerator.GetTypeName (returnType);
			if (types.HasClass (returnType))
				return_type += "*";
			return return_type;
		}

		// TODO override with attribute ? e.g. [ObjC.Selector ("foo")]
		string ImplementMethod (MethodInfo info, string name, bool isExtension = false, PropertyInfo pi = null, bool useTypeNames = false)
		{
			var type = info.DeclaringType;
			var managed_type_name = NameGenerator.GetObjCName (type);

			string objcsig;
			string monosig;
			var managed_name = info.Name;
			var parametersInfo = info.GetParameters ();

			GetSignatures (name, managed_name, (MemberInfo)pi ?? info, parametersInfo, useTypeNames, isExtension, out objcsig, out monosig);

			var builder = new MethodHelper (headers, implementation) {
				AssemblySafeName = type.Assembly.GetName ().Name.Sanitize (),
				IsStatic = info.IsStatic,
				IsExtension = isExtension,
				ReturnType = GetReturnType (type, info.ReturnType),
				ManagedTypeName = type.FullName,
				MetadataToken = info.MetadataToken,
				MonoSignature = monosig,
				ObjCSignature = objcsig,
				ObjCTypeName = managed_type_name,
				IsValueType = type.IsValueType,
				IsVirtual = info.IsVirtual,
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
			return objcsig;
		}

		bool GenerateDefaultValuesWrappers (string name, MethodBase mb)
		{
			// parameters with default values must be at the end and there can be many of them
			var parameters = mb.GetParameters ();
			for (int i = parameters.Length - 1; i >= 0; i--) {
				if (!parameters [i].HasDefaultValue)
					return false;
				GenerateDefaultValuesWrapper (name, mb, parameters, i);
			}
			return true;
		}

		void GenerateDefaultValuesWrapper (string name, MethodBase mb, ParameterInfo[] parameters, int start)
		{
			MethodInfo mi = mb as MethodInfo;
			string objcsig;
			string monosig;
			var parametersInfo = parameters;
			var plist = new List<ParameterInfo> ();
			StringBuilder arguments = new StringBuilder ();
			headers.WriteLine ("/** This is an helper method that inlines the following default values:");
			foreach (var p in parameters) {
				string pName = NameGenerator.GetExtendedParameterName (p, parameters);
				if (arguments.Length == 0) {
					arguments.Append (p.Name.PascalCase ()).Append (':');
				} else
					arguments.Append (' ').Append (p.Name.CamelCase ()).Append (':');
				if (p.Position >= start && p.HasDefaultValue) {
					var raw = FormatRawValue (p.ParameterType, p.RawDefaultValue);
					headers.WriteLine ($" *     ({NameGenerator.GetTypeName (p.ParameterType)}) {pName} = {raw};");
					arguments.Append (raw);
				} else {
					arguments.Append (pName);
					plist.Add (p);
				}
			}
			headers.WriteLine (" *");
			headers.WriteLine ($" *  @see {name}");
			headers.WriteLine (" */");

			if (mi == null)
				name = start == 0 ? "init" : "initWith";
			else
				name = mb.Name.CamelCase ();
			
			GetSignatures (name, mb.Name, mb, plist.ToArray (), false, false, out objcsig, out monosig);
			var type = mb.DeclaringType;
			var builder = new MethodHelper (headers, implementation) {
				IsStatic = mb.IsStatic,
				ReturnType = mi == null ? "nullable instancetype" : GetReturnType (type, mi.ReturnType),
				ObjCSignature = objcsig,
			};
			builder.WriteHeaders ();
			headers.WriteLine ();

			builder.BeginImplementation ();
			if (mi == null || !mi.ReturnType.Is ("System", "Void"))
				implementation.Write ("return [");
			if (mb.IsStatic) {
				implementation.Write (NameGenerator.GetObjCName (mi.DeclaringType));
				implementation.Write (' ');
			} else {
				implementation.Write ("self ");
			}
			if (mi == null)
				name = "initWith";
			implementation.WriteLine ($"{name}{arguments}];");
			builder.EndImplementation ();
		}

		protected override void Generate (ProcessedMethod method)
		{
			var objcsig = ImplementMethod (method.Method, method.BaseName, useTypeNames: method.FallBackToTypeName);

			if (members_with_default_values.Contains (method.Method))
				GenerateDefaultValuesWrappers (objcsig, method.Method);
		}

		void ReturnValue (Type t)
		{
			switch (Type.GetTypeCode (t)) {
			case TypeCode.String:
				implementation.WriteLine ("return mono_embeddinator_get_nsstring ((MonoString *) __result);");
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
				var name = NameGenerator.GetTypeName (t);
				implementation.WriteLine ("void* __unbox = mono_object_unbox (__result);");
				implementation.WriteLine ($"return *(({name}*)__unbox);");
				break;
			case TypeCode.Object:
				if (t.Namespace == "System" && t.Name == "Void")
					return;
				if (!types.HasClass (t) && !types.HasProtocol (t))
					goto default;

				implementation.WriteLine ("if (!__result)");
				implementation.Indent++;
				implementation.WriteLine ("return nil;");
				implementation.Indent--;
				// TODO: cheating by reusing `initForSuper` - maybe a better name is needed
				var tname = NameGenerator.GetTypeName (t);
				if (types.HasProtocol (t))
					tname = "__" + tname + "Wrapper";
				implementation.WriteLine ($"\t{tname}* __peer = [[{tname} alloc] initForSuper];");
				implementation.WriteLine ("__peer->_object = mono_embeddinator_create_object (__result);");
				implementation.WriteLine ("return __peer;");
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

		public static string FormatRawValue (Type t, object o)
		{
			if (o == null)
				return "nil";

			switch (t.Namespace) {
			case "System":
				switch (t.Name) {
				case "String":
					return $"@\"{o}\"";
				case "Single":
					float f = (float)o;
					if (Single.IsNaN (f))
						return "NAN";
					if (Single.IsInfinity (f))
						return "INFINITY";
					return o + "f";
				case "Double":
					double d = (double)o;
					if (Double.IsNaN (d))
						return "NAN";
					if (Double.IsInfinity (d))
						return "INFINITY";
					return o + "d";
				case "UInt32":
					return o + "ul";
				case "Int64":
					return o + "ll";
				case "UInt64":
					return o + "ull";
				}
				break;
			}
			if (t.IsEnum)
				return NameGenerator.GetTypeName (t) + t.GetEnumName (o);
			return o.ToString ();
		}
	}
}
