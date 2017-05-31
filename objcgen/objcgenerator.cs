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
		SourceWriter private_headers = new SourceWriter ();
		SourceWriter implementation = new SourceWriter ();

		public override void Generate ()
		{
			var types = Processor.Types;
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

			private_headers.WriteLine ("#import <Foundation/Foundation.h>");
			private_headers.WriteLine ();

			implementation.WriteLine ("#include \"bindings.h\"");
			implementation.WriteLine ("#include \"bindings-private.h\"");
			implementation.WriteLine ("#include \"glib.h\"");
			implementation.WriteLine ("#include \"objc-support.h\"");
			implementation.WriteLine ("#include \"mono_embeddinator.h\"");
			implementation.WriteLine ("#include \"mono-support.h\"");
			implementation.WriteLine ();

			implementation.WriteLine ("mono_embeddinator_context_t __mono_context;");
			implementation.WriteLine ();

			foreach (var a in Processor.Assemblies)
				implementation.WriteLine ($"MonoImage* __{a.SafeName}_image;");
			implementation.WriteLine ();

			foreach (var t in types.Where ((arg) => arg.IsClass))
				implementation.WriteLine ($"static MonoClass* {t.ObjCName}_class = nil;");
			foreach (var t in types.Where ((arg) => arg.IsProtocol)) {
				var pname = t.TypeName;
				headers.WriteLine ($"@protocol {pname};");
				implementation.WriteLine ($"@class __{pname}Wrapper;");
			}
			headers.WriteLine ();
			headers.WriteLine ("NS_ASSUME_NONNULL_BEGIN");
			headers.WriteLine ();
			implementation.WriteLine ();

			implementation.WriteLine ("static void __initialize_mono ()");
			implementation.WriteLine ("{");
			implementation.Indent++;
			implementation.WriteLine ("if (__mono_context.domain)");
			implementation.Indent++;
			implementation.WriteLine ("return;");
			implementation.Indent--;
			implementation.WriteLine ("mono_embeddinator_init (&__mono_context, \"mono_embeddinator_binding\");");
			implementation.WriteLine ("mono_embeddinator_install_assembly_load_hook (&mono_embeddinator_find_assembly_in_bundle);");
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
			if (name == "mscorlib") {
				// skip extra logic - we know mscorlib is already loaded into memory
				implementation.WriteLine ($"__{name}_image = mono_get_corlib ();");
			} else {
				implementation.WriteLine ($"__{name}_image = mono_embeddinator_load_assembly (&__mono_context, \"{originalName}.dll\");");
			}
			implementation.WriteLine ($"assert (__{name}_image && \"Could not load the assembly '{originalName}.dll'.\");");
			var categories = extensions_methods.Keys;
			if (categories.Count > 0) {
				implementation.WriteLine ("// we cannot use `+initialize` inside categories as they would replace the original type code");
				implementation.WriteLine ("// since there should not be tons of them we're pre-loading them when loading the assembly");
				foreach (var definedType in extensions_methods.Keys) {
					if (definedType.Assembly != a.Assembly)
						continue;
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
			var types = Processor.Types;

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

		void GenerateCategory (Type definedType, Type extendedType, List<ProcessedMethod> methods)
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
				ImplementMethod (mi);
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
			var pbuilder = new ProtocolHelper (headers, implementation, private_headers) {
				AssemblyQualifiedName = t.AssemblyQualifiedName,
				AssemblyName = type.Assembly.SafeName,
				ProtocolName = type.TypeName,
				Namespace = t.Namespace,
				ManagedName = t.Name,
				MetadataToken = t.MetadataToken,
			};
			pbuilder.BeginHeaders ();

			// no need to iterate constructors or fields as they cannot be part of net interfaces
			// do not generate implementations for protocols
			implementation.Enabled = false;

			if (type.HasProperties) {
				headers.WriteLine ();
				foreach (var pi in type.Properties)
					Generate (pi);
			}

			if (type.HasMethods) {
				headers.WriteLine ();
				foreach (var mi in type.Methods)
					Generate (mi);
			}

			pbuilder.EndHeaders ();

			// wrappers are internal so not part of the headers
			headers.Enabled = false;
			implementation.Enabled = true;

			pbuilder.BeginImplementation ();

			if (type.HasProperties) {
				implementation.WriteLine ();
				foreach (var pi in type.Properties)
					Generate (pi);
			}

			if (type.HasMethods) {
				implementation.WriteLine ();
				foreach (var mi in type.Methods)
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
				if (Processor.Types.HasProtocol (i))
					conformed_protocols.Add (NameGenerator.GetObjCName (i));
			}

			var tbuilder = new ClassHelper (headers, implementation) {
				AssemblyQualifiedName = t.AssemblyQualifiedName,
				AssemblyName = aname,
				BaseTypeName = NameGenerator.GetTypeName (t.BaseType),
				Name = NameGenerator.GetTypeName (t),
				Namespace = t.Namespace,
				ManagedName = (t.DeclaringType != null ? t.DeclaringType.Name + "/" : "") + t.Name,
				Protocols = conformed_protocols,
				IsBaseTypeBound = Processor.Types.HasClass (t.BaseType),
				IsStatic = t.IsSealed && t.IsAbstract,
				MetadataToken = t.MetadataToken,
			};

			tbuilder.BeginHeaders ();
			tbuilder.BeginImplementation ();

			var default_init = false;
			if (type.HasConstructors) {
				foreach (var ctor in type.Constructors) {
					var pcount = ctor.Parameters.Length;
					default_init |= pcount == 0;

					var parameters = ctor.Parameters;
					string name = ctor.BaseName;
					string signature = ".ctor()";
					if (parameters.Length > 0) {
						name = ctor.ObjCSignature;
						signature = ctor.MonoSignature;
					}

					if (ctor.Unavailable) {
						headers.WriteLine ("/** This initializer is not available as it was not re-exposed from the base type");
						headers.WriteLine (" *  For more details consult https://github.com/mono/Embeddinator-4000/blob/master/docs/ObjC.md#constructors-vs-initializers");
						headers.WriteLine (" */");
						headers.WriteLine ($"- (nullable instancetype){name} NS_UNAVAILABLE;");
						headers.WriteLine ();
						continue;
					}

					if (ctor.ConstructorType == ConstructorType.DefaultValueWrapper) {
						default_init |= ctor.FirstDefaultParameter == 0;
						GenerateDefaultValuesWrapper (ctor);
						continue;
					}

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
					if (Processor.Types.HasClass (t.BaseType))
						implementation.WriteLine ("return self = [super initForSuper];");
					else
						implementation.WriteLine ("return self = [super init];");
					builder.EndImplementation ();

					headers.WriteLine ();
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
				if (HasClass (t.BaseType))
					implementation.WriteLine ("return self = [super initForSuper];");
				else
					implementation.WriteLine ("return self = [super init];");
				builder.EndImplementation ();

				headers.WriteLine ();
				default_init = true;
			}

			if (!default_init || static_type)
				tbuilder.DefineNoDefaultInit ();
	
			if (type.HasProperties) {
				headers.WriteLine ();
				foreach (var pi in type.Properties)
					Generate (pi);
			}

			if (type.HasFields) {
				headers.WriteLine ();
				foreach (var fi in type.Fields)
					Generate (fi);
			}

			List<ProcessedProperty> s;
			if (subscriptProperties.TryGetValue (t, out s)) {
				headers.WriteLine ();
				foreach (var si in s)
					GenerateSubscript (si);
			}

			if (type.HasMethods) {
				headers.WriteLine ();
				foreach (var mi in type.Methods)
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
					MonoSignature = $"CompareTo({NameGenerator.GetMonoName (pt)})"
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

		string GenerateArgumentArrayPost (string parameterName, string argumentName, Type t)
		{
			var postwriter = new SourceWriter {
				Indent = 1
			};
			var typecode = Type.GetTypeCode (t);
			var parrlength = $"__p{parameterName}length";
			var presobj = $"__p{parameterName}resobj";
			var pindex = $"__p{parameterName}residx";
			var pbuff = $"__p{parameterName}buf";
			var presarrval = $"__p{parameterName}resarrval";
			var ptemp = $"__p{parameterName}tmpobj";
			var presarr = $"__{parameterName}arr";

			postwriter.WriteLine ();
			postwriter.WriteLine ($"if ({presarr}) {{");
			postwriter.Indent++;
			postwriter.WriteLine ($"int {parrlength} = mono_array_length ({presarr});");

			if (typecode != TypeCode.Byte) {
				postwriter.WriteLine ($"__strong id * {pbuff} = (id __strong *) calloc ({parrlength}, sizeof (id));");
				postwriter.WriteLine ($"id {presobj};");
				postwriter.WriteLine ($"int {pindex};");
				postwriter.WriteLine ();
				postwriter.WriteLine ($"for ({pindex} = 0; {pindex} < {parrlength}; {pindex}++) {{");
				postwriter.Indent++;
			}

			switch (typecode) {
			case TypeCode.Boolean:
			case TypeCode.Char:
			case TypeCode.Double:
			case TypeCode.Single:
			case TypeCode.SByte:
			case TypeCode.Int16:
			case TypeCode.Int32:
			case TypeCode.Int64:
			case TypeCode.UInt16:
			case TypeCode.UInt32:
			case TypeCode.UInt64:
				var ctype = NameGenerator.GetTypeName (t);
				string ctypep;
				if (typecode == TypeCode.SByte)
					ctypep = "Char"; // GetTypeName returns signed char
				else
					ctypep = ctype.PascalCase (true);
				postwriter.WriteLine ($"{ctype} {presarrval} = mono_array_get ({presarr}, {ctype}, {pindex});");
				postwriter.WriteLine ($"{presobj} = [NSNumber numberWith{ctypep}:{presarrval}];");
				break;
			case TypeCode.Decimal:
				postwriter.WriteLine ($"MonoDecimal {presarrval} = mono_array_get ({presarr}, MonoDecimal, {pindex});");
				postwriter.WriteLine ($"{presobj} = mono_embeddinator_get_nsdecimalnumber (&{presarrval});");
				break;
			case TypeCode.DateTime:
				postwriter.WriteLine ($"E4KDateTime {presarrval} = mono_array_get ({presarr}, E4KDateTime, {pindex});");
				postwriter.WriteLine ($"{presobj} = mono_embeddinator_get_nsdate (&{presarrval});");
				break;
			case TypeCode.Byte:
				postwriter.WriteLine ($"NSData* {presobj} = [NSData dataWithBytes:mono_array_addr ({presarr}, unsigned char, 0) length:{parrlength}];");
				break;
			case TypeCode.String:
				postwriter.WriteLine ($"MonoString* {presarrval} = mono_array_get ({presarr}, MonoString *, {pindex});");
				postwriter.WriteLine ($"if ({presarrval})");
				postwriter.Indent++;
				postwriter.WriteLine ($"{presobj} = mono_embeddinator_get_nsstring ({presarrval});");
				postwriter.Indent--;
				postwriter.WriteLine ("else");
				postwriter.Indent++;
				postwriter.WriteLine ($"{presobj} = [NSNull null];");
				postwriter.Indent--;
				break;
			case TypeCode.Object:
				var tname = NameGenerator.GetTypeName (t);
				if (HasProtocol (t))
					tname = "__" + tname + "Wrapper";
				postwriter.WriteLine ($"MonoObject* {presarrval} = mono_array_get ({presarr}, MonoObject *, {pindex});");
				postwriter.WriteLine ($"if ({presarrval}) {{");
				postwriter.Indent++;
				postwriter.WriteLine ($"{tname}* {ptemp} = [[{tname} alloc] initForSuper];");
				postwriter.WriteLine ($"{ptemp}->_object = mono_embeddinator_create_object ({presarrval});");
				postwriter.WriteLine ($"{presobj} = {ptemp};");
				postwriter.Indent--;
				postwriter.WriteLine ("} else");
				postwriter.Indent++;
				postwriter.WriteLine ($"{presobj} = [NSNull null];");
				postwriter.Indent--;
				break;
			default:
				throw new NotImplementedException ($"Converting type {t.Name} to a native type name");
			}

			if (typecode == TypeCode.Byte)
				postwriter.WriteLine ($"*{parameterName} = {presobj};");
			else {
				postwriter.WriteLine ($"{pbuff}[{pindex}] = {presobj};");
				postwriter.Indent--;
				postwriter.WriteLine ("}");
				postwriter.WriteLine ($"*{parameterName} = [[NSArray alloc] initWithObjects: {pbuff} count: {parrlength}];");
				postwriter.WriteLine ($"for ({pindex} = 0; {pindex} < {parrlength}; {pindex}++)");
				postwriter.Indent++;
				postwriter.WriteLine ($"{pbuff} [{pindex}] = nil;");
				postwriter.Indent--;
				postwriter.WriteLine ($"free ({pbuff});");
			}
			postwriter.Indent--;
			postwriter.WriteLine ("}");
			postwriter.WriteLine ();
			return postwriter.ToString ();
		}

		void GenerateArgumentArray (string parameterName, string argumentName, Type t, bool is_by_ref, ref StringBuilder post)
		{
			Type type = t.GetElementType ();
			var typeCode = Type.GetTypeCode (type);
			var pnameIdx = $"__{parameterName}idx";
			var pnameArr = $"__{parameterName}arr";
			var pnameRet = $"__{parameterName}ret";
			var pnameLength = $"__{parameterName}length";
			string arrayCreator = GetArrayCreator (parameterName, type, is_by_ref);

			if (is_by_ref)
				implementation.WriteLine ($"MonoArray* __{parameterName}arr = nil;");

			implementation.WriteLine ($"if (!{(is_by_ref ? "*" : string.Empty)}{parameterName})");
			implementation.Indent++;
			implementation.WriteLine ($"{argumentName} = {(is_by_ref ? $"&__{parameterName}arr" : "nil")};");
			implementation.Indent--;
			implementation.WriteLine ("else {");
			implementation.Indent++;
			implementation.WriteLine ($"uintptr_t {pnameLength} = [{(is_by_ref ? "*" : string.Empty)}{parameterName} {(typeCode == TypeCode.Byte ? "length" : "count")}];");

			implementation.WriteLine (arrayCreator);

			if (typeCode != TypeCode.Byte) {
				implementation.WriteLine ($"int {pnameIdx};");
				implementation.WriteLine ($"for ({pnameIdx} = 0; {pnameIdx} < __{parameterName}length; {pnameIdx}++) {{");
				implementation.Indent++;
			}

			switch (typeCode) {
			case TypeCode.Boolean:
			case TypeCode.Char:
			case TypeCode.SByte:
			case TypeCode.Int16:
			case TypeCode.Int32:
			case TypeCode.Int64:
			case TypeCode.UInt16:
			case TypeCode.UInt32:
			case TypeCode.UInt64:
			case TypeCode.Single:
			case TypeCode.Double:
				var typeName = NameGenerator.GetTypeName (type);
				string returnValue;
				if (typeCode == TypeCode.SByte)
					returnValue = $"charValue"; // GetTypeName returns signed char
				else
					returnValue = $"{typeName.CamelCase (true)}Value";

				implementation.WriteLine ($"NSNumber* {pnameRet} = {(is_by_ref ? $"(*{parameterName})" : parameterName)}[{pnameIdx}];");
				implementation.WriteLine ($"if (!{pnameRet} || [{pnameRet} isKindOfClass:[NSNull class]])");
				implementation.Indent++;
				implementation.WriteLine ($"continue;");
				implementation.Indent--;
				implementation.WriteLine ($"mono_array_set ({pnameArr}, {typeName}, {pnameIdx}, {pnameRet}.{returnValue});");
				break;
			case TypeCode.Decimal:
				var pparname = is_by_ref ? $"(*{parameterName})" : parameterName;
				implementation.WriteLine ($"NSDecimalNumber* {pnameRet} = {pparname}[{pnameIdx}];");
				implementation.WriteLine ($"if (!{pnameRet} || [{pnameRet} isKindOfClass:[NSNull class]])");
				implementation.Indent++;
				implementation.WriteLine ($"continue;");
				implementation.Indent--;
				implementation.WriteLine ($"mono_array_set ({pnameArr}, MonoDecimal, {pnameIdx}, mono_embeddinator_get_system_decimal ({pnameRet}, &__mono_context));");
				break;
			case TypeCode.DateTime:
				var dtparname = is_by_ref ? $"(*{parameterName})" : parameterName;
				implementation.WriteLine ($"NSDate* {pnameRet} = {dtparname}[{pnameIdx}];");
				implementation.WriteLine ($"if (!{pnameRet} || [{pnameRet} isKindOfClass:[NSNull class]])");
				implementation.Indent++;
				implementation.WriteLine ($"continue;");
				implementation.Indent--;
				implementation.WriteLine ($"mono_array_set ({pnameArr}, E4KDateTime, {pnameIdx}, mono_embeddinator_get_system_datetime ({pnameRet}, &__mono_context));");
				break;
			case TypeCode.String:
				implementation.WriteLine ($"NSString* {pnameRet} = {(is_by_ref ? $"(*{parameterName})" : parameterName)}[{pnameIdx}];");
				implementation.WriteLine ($"if (!{pnameRet} || [{pnameRet} isKindOfClass:[NSNull class]])");
				implementation.Indent++;
				implementation.WriteLine ($"mono_array_set ({pnameArr}, MonoString *, {pnameIdx}, NULL);");
				implementation.Indent--;
				implementation.WriteLine ("else");
				implementation.Indent++;
				implementation.WriteLine ($"mono_array_set ({pnameArr}, MonoString *, {pnameIdx}, mono_string_new (__mono_context.domain, [{pnameRet} UTF8String]));");
				implementation.Indent--;
				break;
			case TypeCode.Byte:
				implementation.WriteLine ($"int esize = mono_array_element_size (mono_object_get_class ((MonoObject *){pnameArr}));");
				implementation.WriteLine ($"char* buff = mono_array_addr_with_size ({pnameArr}, esize, 0);");
				implementation.WriteLine ($"[{(is_by_ref ? "*" : string.Empty)}{parameterName} getBytes:buff length:{pnameLength}];");
				break;
			case TypeCode.Object:
				var objcName = NameGenerator.GetObjCName (type);
				bool hasClass = false, hasProtocol = false;
				if (type.IsInterface)
					hasProtocol = HasProtocol (type);
				else
					hasClass = HasClass (type);

				if (hasClass)
					implementation.WriteLine ($"{objcName}* {pnameRet} = {(is_by_ref ? $"(*{parameterName})" : parameterName)}[{pnameIdx}];");
				else if (hasProtocol)
					implementation.WriteLine ($"id<{objcName}> {pnameRet} = {(is_by_ref ? $"(*{parameterName})" : parameterName)}[{pnameIdx}];");
				else
					goto default;
				implementation.WriteLine ($"if (!{pnameRet} || [{pnameRet} isKindOfClass:[NSNull class]])");
				implementation.Indent++;
				implementation.WriteLine ($"mono_array_set ({pnameArr}, MonoObject *, {pnameIdx}, NULL);");
				implementation.Indent--;
				implementation.WriteLine ("else");
				implementation.Indent++;
				if (hasClass)
					implementation.WriteLine ($"mono_array_set ({pnameArr}, MonoObject *, {pnameIdx}, mono_gchandle_get_target ({pnameRet}->_object->_handle));");
				else if (hasProtocol)
					implementation.WriteLine ($"mono_array_set ({pnameArr}, MonoObject *, {pnameIdx}, mono_embeddinator_get_object ({pnameRet}, true));");
				break;
			default:
				throw new NotImplementedException ($"Converting type {type.FullName} to mono code");
			}

			if (typeCode != TypeCode.Byte) {
				implementation.Indent--;
				implementation.WriteLine ("}");
			}
			implementation.WriteLine ($"{argumentName} = {(is_by_ref ? "&" : string.Empty)}{pnameArr};");
			implementation.Indent--;
			implementation.WriteLine ("}");

			if (is_by_ref)
				post.AppendLine (GenerateArgumentArrayPost (parameterName, argumentName, type));
		}

		void GenerateArgument (string paramaterName, string argumentName, Type t, ref StringBuilder post)
		{
			var is_by_ref = t.IsByRef;
			if (is_by_ref)
				t = t.GetElementType ();

			if (t.IsArray) {
				GenerateArgumentArray (paramaterName, argumentName, t, is_by_ref, ref post);
				return;
			}
			
			switch (Type.GetTypeCode (t)) {
			case TypeCode.String:
				if (is_by_ref) {
					implementation.WriteLine ($"MonoString* __string = *{paramaterName} ? mono_string_new (__mono_context.domain, [*{paramaterName} UTF8String]) : nil;");
					implementation.WriteLine ($"{argumentName} = &__string;");
					post.AppendLine ($"*{paramaterName} = mono_embeddinator_get_nsstring (__string);");
				} else
					implementation.WriteLine ($"{argumentName} = {paramaterName} ? mono_string_new (__mono_context.domain, [{paramaterName} UTF8String]) : nil;");
				break;
			case TypeCode.Decimal:
				implementation.WriteLine ($"MonoDecimal __mdec = mono_embeddinator_get_system_decimal ({(is_by_ref ? "*" : string.Empty)}{paramaterName}, &__mono_context);");
				implementation.WriteLine ($"{argumentName} = &__mdec;");
				if (is_by_ref)
					post.AppendLine ($"*{paramaterName} = mono_embeddinator_get_nsdecimalnumber (&__mdec);");
				break;
			case TypeCode.DateTime:
				implementation.WriteLine ($"E4KDateTime __mdatetime = mono_embeddinator_get_system_datetime ({(is_by_ref ? "*" : string.Empty)}{paramaterName}, &__mono_context);");
				implementation.WriteLine ($"{argumentName} = &__mdatetime;");
				if (is_by_ref)
					post.AppendLine ($"*{paramaterName} = mono_embeddinator_get_nsdate (&__mdatetime);");
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
				if (HasClass (t))
					implementation.WriteLine ($"{argumentName} = {paramaterName} ? mono_gchandle_get_target ({paramaterName}->_object->_handle): nil;");
				else if (HasProtocol (t))
					implementation.WriteLine ($"{argumentName} = {paramaterName} ? mono_embeddinator_get_object ({paramaterName}, true) : nil;");
				else
					throw new NotImplementedException ($"Converting type {t.FullName} to mono code");
				break;
			}
		}

		protected override void Generate (ProcessedProperty property)
		{
			var getter = property.GetMethod;
			var setter = property.SetMethod;
			// setter-only properties are handled as methods (and should not reach this code)
			if (getter == null && setter != null)
				throw new EmbeddinatorException (99, "Internal error `setter only`. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues");

			headers.Write ("@property (nonatomic");
			if (getter.Method.IsStatic)
				headers.Write (", class");
			if (setter == null)
				headers.Write (", readonly");
			var pt = property.Property.PropertyType;
			var property_type = NameGenerator.GetTypeName (pt);
			if (HasClass (pt))
				property_type += " *";

			var spacing = property_type [property_type.Length - 1] == '*' ? string.Empty : " ";
			headers.WriteLine ($") {property_type}{spacing}{property.Name};");

			ImplementMethod (property.GetMethod);
			if (setter == null)
				return;

			ImplementMethod (property.SetMethod);
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
			var bound = HasClass (ft);
			if (bound && ft.IsValueType)
				headers.Write (", nonnull");

			var field_type = NameGenerator.GetTypeName (ft);
			if (bound)
				field_type += " *";


			var spacing = field_type [field_type.Length - 1] == '*' ? string.Empty : " ";
			headers.WriteLine ($") {field_type}{spacing}{field.Name};");

			// it's similar, but different from implementing a method

			var type = fi.DeclaringType;
			var managed_type_name = NameGenerator.GetObjCName (type);
			var return_type = GetReturnType (type, fi.FieldType);

			implementation.Write (fi.IsStatic ? '+' : '-');
			implementation.WriteLine ($" ({return_type}) {field.GetterName}");
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
			if (HasClass (ft)) {
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
			implementation.WriteLine ($" (void) {field.SetterName}:({field_type})value");
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
			if (HasProtocol (returnType))
				return "id<" + NameGenerator.GetTypeName (returnType) + ">";
			if (declaringType == returnType)
				return "instancetype";

			var return_type = NameGenerator.GetTypeName (returnType);
			if (HasClass (returnType))
				return_type += "*";
			return return_type;
		}

		// TODO override with attribute ? e.g. [ObjC.Selector ("foo")]
		string ImplementMethod (ProcessedMethod method)
		{
			MethodInfo info = method.Method;

			var type = info.DeclaringType;
			var managed_type_name = NameGenerator.GetObjCName (type);

			string objcsig = method.ObjCSignature;

			var builder = new MethodHelper (headers, implementation) {
				AssemblySafeName = type.Assembly.GetName ().Name.Sanitize (),
				IsStatic = info.IsStatic,
				IsExtension = method.IsExtension,
				ReturnType = GetReturnType (type, info.ReturnType),
				ManagedTypeName = type.FullName,
				MetadataToken = info.MetadataToken,
				MonoSignature = method.MonoSignature,
				ObjCSignature = objcsig,
				ObjCTypeName = managed_type_name,
				IsValueType = type.IsValueType,
				IsVirtual = info.IsVirtual && !info.IsFinal,
			};

			if (!method.IsPropertyImplementation)
				builder.WriteHeaders ();
			
			builder.BeginImplementation ();
			builder.WriteMethodLookup ();

			var parametersInfo = method.Parameters;
			string postInvoke = String.Empty;
			var args = "nil";
			if (parametersInfo.Length > 0) {
				Generate (parametersInfo, method.IsExtension, out postInvoke);
				args = "__args";
			}

			builder.WriteInvoke (args);

			// ref and out parameters might need to be converted back
			implementation.Write (postInvoke);
			ReturnValue (info.ReturnType);
			builder.EndImplementation ();
			return objcsig;
		}

		void GenerateDefaultValuesWrapper (ProcessedMemberWithParameters member)
		{
			ProcessedMethod method = member as ProcessedMethod;
			ProcessedConstructor ctor = member as ProcessedConstructor;
			if (method == null && ctor == null)
				throw new NotSupportedException ("GenerateDefaultValuesWrapper did not get ctor or method?");

			MethodBase mb = method != null ? (MethodBase)method.Method : ctor.Constructor;
			MethodInfo mi = mb as MethodInfo;

			var plist = new List<ParameterInfo> ();
			StringBuilder arguments = new StringBuilder ();
			headers.WriteLine ("/** This is an helper method that inlines the following default values:");
			foreach (var p in member.Parameters) {
				string pName = NameGenerator.GetExtendedParameterName (p, member.Parameters);
				if (arguments.Length == 0) {
					arguments.Append (p.Name.PascalCase ()).Append (':');
				} else
					arguments.Append (' ').Append (p.Name.CamelCase ()).Append (':');
				if (p.Position >= member.FirstDefaultParameter && p.HasDefaultValue) {
					var raw = FormatRawValue (p.ParameterType, p.RawDefaultValue);
					headers.WriteLine ($" *     ({NameGenerator.GetTypeName (p.ParameterType)}) {pName} = {raw};");
					arguments.Append (raw);
				} else {
					arguments.Append (pName);
					plist.Add (p);
				}
			}

			string name = member.BaseName;
			string objcsig = member.ObjCSignature;

			var type = mb.DeclaringType;

			headers.WriteLine (" *");
			headers.WriteLine ($" *  @see {objcsig}");
			headers.WriteLine (" */");
				
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
			MethodHelper builder;
			switch (method.MethodType) {
			case MethodType.DefaultValueWrapper:
				GenerateDefaultValuesWrapper (method);
				return;
			case MethodType.NSObjectProcotolHash:
				builder = new HashHelper (method, headers, implementation);
				break;
			case MethodType.NSObjectProcotolIsEqual:
				builder = new EqualsHelper (method, headers, implementation);
				break;
			case MethodType.IEquatable:
				builder = new EquatableHelper (method, headers, implementation);
				break;
			default:
				ImplementMethod (method);
				return;
			}
			builder.WriteHeaders ();
			builder.WriteImplementation ();
		}

		void ReturnArrayValue (Type t)
		{
			var typecode = Type.GetTypeCode (t);
			implementation.WriteLine ("MonoArray* __resarr = (MonoArray *) __result;");
			implementation.WriteLine ("if (!__resarr)");
			implementation.Indent++;
			implementation.WriteLine ("return nil;");
			implementation.Indent--;
			implementation.WriteLine ("int __resarrlength = mono_array_length (__resarr);");

			if (typecode != TypeCode.Byte) {
				implementation.WriteLine ("__strong id * __resarrbuf = (id __strong *) calloc (__resarrlength, sizeof (id));");
				implementation.WriteLine ("id __resobj;");
				implementation.WriteLine ("int __residx;");
				implementation.WriteLine ();
				implementation.WriteLine ("for (__residx = 0; __residx < __resarrlength; __residx++) {");
				implementation.Indent++;
			}

			switch (typecode) {
			case TypeCode.Boolean:
			case TypeCode.Char:
			case TypeCode.Double:
			case TypeCode.Single:
			case TypeCode.SByte:
			case TypeCode.Int16:
			case TypeCode.Int32:
			case TypeCode.Int64:
			case TypeCode.UInt16:
			case TypeCode.UInt32:
			case TypeCode.UInt64:
				var ctype = NameGenerator.GetTypeName (t);
				string ctypep;
				if (typecode == TypeCode.SByte)
					ctypep = "Char"; // GetTypeName returns signed char
				else
					ctypep = ctype.PascalCase (true);
				implementation.WriteLine ($"{ctype} __resarrval = mono_array_get (__resarr, {ctype}, __residx);");
				implementation.WriteLine ($"__resobj = [NSNumber numberWith{ctypep}:__resarrval];");
				break;
			case TypeCode.Decimal:
				implementation.WriteLine ($"MonoDecimal __resarrval = mono_array_get (__resarr, MonoDecimal, __residx);");
				implementation.WriteLine ($"__resobj = mono_embeddinator_get_nsdecimalnumber (&__resarrval);");
				break;
			case TypeCode.DateTime:
				implementation.WriteLine ($"E4KDateTime __resarrval = mono_array_get (__resarr, E4KDateTime, __residx);");
				implementation.WriteLine ($"__resobj = mono_embeddinator_get_nsdate (&__resarrval);");
				break;
			case TypeCode.Byte:
				implementation.WriteLine ("NSData* __resobj = [NSData dataWithBytes:mono_array_addr (__resarr, unsigned char, 0) length:__resarrlength];");
				break;
			case TypeCode.String:
				implementation.WriteLine ("MonoString* __resarrval = mono_array_get (__resarr, MonoString *, __residx);");
				implementation.WriteLine ("if (__resarrval)");
				implementation.Indent++;
				implementation.WriteLine ("__resobj = mono_embeddinator_get_nsstring (__resarrval);");
				implementation.Indent--;
				implementation.WriteLine ("else");
				implementation.Indent++;
				implementation.WriteLine ("__resobj = [NSNull null];");
				implementation.Indent--;
				break;
			case TypeCode.Object:
				var tname = NameGenerator.GetTypeName (t);
				if (HasProtocol (t))
					tname = "__" + tname + "Wrapper";
				implementation.WriteLine ("MonoObject* __resarrval = mono_array_get (__resarr, MonoObject *, __residx);");
				implementation.WriteLine ("if (__resarrval) {");
				implementation.Indent++;
				implementation.WriteLine ($"{tname}* __tmpobj = [[{tname} alloc] initForSuper];");
				implementation.WriteLine ("__tmpobj->_object = mono_embeddinator_create_object (__resarrval);");
				implementation.WriteLine ("__resobj = __tmpobj;");
				implementation.Indent--;
				implementation.WriteLine ("} else");
				implementation.Indent++;
				implementation.WriteLine ("__resobj = [NSNull null];");
				implementation.Indent--;
				break;
			default:
				throw new NotImplementedException ($"Converting type {t.Name} to a native type name");
			}

			if (typecode == TypeCode.Byte)
				implementation.WriteLine ("return __resobj;");
			else {
				implementation.WriteLine ("__resarrbuf[__residx] = __resobj;");
				implementation.Indent--;
				implementation.WriteLine ("}");
				implementation.WriteLine ("NSArray* __retarr = [[NSArray alloc] initWithObjects: __resarrbuf count: __resarrlength];");
				implementation.WriteLine ("for (__residx = 0; __residx < __resarrlength; __residx++)");
				implementation.Indent++;
				implementation.WriteLine ("__resarrbuf [__residx] = nil;");
				implementation.Indent--;
				implementation.WriteLine ("free(__resarrbuf);");
				implementation.WriteLine ("return __retarr;");
			}
		}

		void ReturnValue (Type t)
		{
			if (t.IsArray) {
				ReturnArrayValue (t.GetElementType ());
				return;
			}

			switch (Type.GetTypeCode (t)) {
			case TypeCode.String:
				implementation.WriteLine ("return mono_embeddinator_get_nsstring ((MonoString *) __result);");
				break;
			case TypeCode.Decimal:
				implementation.WriteLine ("void* __unboxedresult = mono_object_unbox (__result);");
				implementation.WriteLine ("return mono_embeddinator_get_nsdecimalnumber (__unboxedresult);");
				break;
			case TypeCode.DateTime:
				implementation.WriteLine ("void* __unboxedresult = mono_object_unbox (__result);");
				implementation.WriteLine ("return mono_embeddinator_get_nsdate (__unboxedresult);");
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
				if (!HasClass (t) && !HasProtocol (t))
					goto default;

				implementation.WriteLine ("if (!__result)");
				implementation.Indent++;
				implementation.WriteLine ("return nil;");
				implementation.Indent--;
				// TODO: cheating by reusing `initForSuper` - maybe a better name is needed
				var tname = NameGenerator.GetTypeName (t);
				if (HasProtocol (t))
					tname = "__" + tname + "Wrapper";
				implementation.WriteLine ($"{tname}* __peer = [[{tname} alloc] initForSuper];");
				implementation.WriteLine ("__peer->_object = mono_embeddinator_create_object (__result);");
				implementation.WriteLine ("return __peer;");
				break;
			default:
				throw new NotImplementedException ($"Returning type {t.Name} from native code");
			}
		}

		public override void Write (string outputDirectory)
		{
			WriteFile (Path.Combine (outputDirectory, "bindings.h"), headers.ToString ());
			WriteFile (Path.Combine (outputDirectory, "bindings-private.h"), private_headers.ToString ());
			WriteFile (Path.Combine (outputDirectory, "bindings.m"), implementation.ToString ());
			base.Write (outputDirectory);
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

		public static string GetArrayCreator (string parameterName, Type type, bool is_by_ref)
		{
			string arrayCreator = $"{(is_by_ref ? string.Empty : "MonoArray * ")}__{parameterName}arr = mono_array_new (__mono_context.domain, {{0}}, __{parameterName}length);";

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
			case TypeCode.DateTime:
				return string.Format (arrayCreator, "mono_embeddinator_get_datetime_class ()");
			default:
				throw new NotImplementedException ($"Converting type {type.FullName} to mono class");
			}
		}
	}
}
