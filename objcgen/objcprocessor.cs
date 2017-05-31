using System;
using System.Collections.Generic;
using System.Linq;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

using Embeddinator;

namespace ObjC {

	public partial class ObjCProcessor : Processor {

		HashSet<Type> unsupported = new HashSet<Type> ();

		bool IsNSObjectSubclass (Type t)
		{
			if (t.Name == "Object" && t.Namespace == "System")
				return false;
			
			if (t.Name == "NSObject" && t.Namespace == "Foundation")
				return true;

			if (t.BaseType == null)
				return false;

			return IsNSObjectSubclass (t.BaseType);
		}

		// extras - on demand only
		ProcessedAssembly mscorlib_assembly;
		ProcessedType system_decimal;
		ProcessedType system_iformatprovider;
		ProcessedType system_timespan;
		ProcessedType system_globalization_timespanstyles;
		ProcessedType system_datetime;

		ProcessedAssembly GetMscorlib (Type t)
		{
			var a = t.Assembly;
			if (a.GetName ().Name != "mscorlib")
				throw ErrorHelper.CreateError (99, "Internal error: incorrect assembly for `{t}`. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues)");

			if (mscorlib_assembly == null) {
				mscorlib_assembly = new ProcessedAssembly (a) {
					UserCode = false,
				};
			}
			return mscorlib_assembly;
		}

		bool AddDecimalSupport (Type t)
		{
			if (system_decimal != null)
				return true;
			var corlib = GetMscorlib (t);
			system_decimal = new ProcessedType (t) {
				Assembly = corlib,
				// this is tracked because the linker (if enabled) needs to be aware of the requirement
				// but we do not want any code to be generated (it's referenced only from native/glue code)
				IsNativeReference = true,
				Methods = new List<ProcessedMethod> (),
			};
			// we don't want to being everything from System.Decimal, but only the bits the support code can call
			var string_type = corlib.Assembly.GetType ("System.String");
			var iformatprovider_type = corlib.Assembly.GetType ("System.IFormatProvider");
			var parse = t.GetMethod ("Parse", new Type [] { string_type, iformatprovider_type });
			system_decimal.Methods.Add (new ProcessedMethod (parse, this, system_decimal));
			var tostring = t.GetMethod ("ToString", new Type [] { iformatprovider_type });
			system_decimal.Methods.Add (new ProcessedMethod (tostring, this, system_decimal));
			AddExtraType (system_decimal);
			return true;
		}

		bool AddDateTimeSupport (Type t)
		{
			if (system_datetime != null)
				return true;

			var corlib = GetMscorlib (t);
			system_datetime = new ProcessedType (t) {
				Assembly = corlib,
				// this is tracked because the linker (if enabled) needs to be aware of the requirement
				// but we do not want any code to be generated (it's referenced only from native/glue code)
				IsNativeReference = true,
				Methods = new List<ProcessedMethod> (),
				Properties = new List<ProcessedProperty> (),
				Constructors = new List<ProcessedConstructor> (),
			};
			var ticks = t.GetProperty ("Ticks");
			system_datetime.Properties.Add (new ProcessedProperty (ticks, this, system_datetime));

			var kind = t.GetProperty ("Kind");
			system_datetime.Properties.Add (new ProcessedProperty (kind, this, system_datetime));

			var dtk = corlib.Assembly.GetType ("System.DateTimeKind");
			var longT = corlib.Assembly.GetType ("System.Int64");
			var ctorLongKind = t.GetConstructor (new Type [] { longT, dtk });
			system_datetime.Constructors.Add (new ProcessedConstructor (ctorLongKind, this, system_datetime));

			var toUniversalTime = t.GetMethod ("ToUniversalTime");
			system_datetime.Methods.Add (new ProcessedMethod (toUniversalTime, this, system_datetime));

			AddExtraType (system_datetime);
			return true;
		}

		bool IsSupported (Type t)
		{
			if (t.IsByRef)
				return IsSupported (t.GetElementType ());

			if (t.IsArray)
				return IsSupported (t.GetElementType ());

			if (unsupported.Contains (t))
				return false;

			if (t.IsPointer) {
				Delayed.Add (ErrorHelper.CreateWarning (1010, $"Type `{t}` is not generated because `unsafe pointers` are not supported."));
				unsupported.Add (t);
				return false;
			}

			if (t.IsGenericParameter || t.IsGenericType) {
				Delayed.Add (ErrorHelper.CreateWarning (1010, $"Type `{t}` is not generated because `generics` are not supported."));
				unsupported.Add (t);
				return false;
			}

			switch (t.Namespace) {
			case "System":
				switch (t.Name) {
				case "Object": // we cannot accept arbitrary NSObject (which we might not have bound) into mono
				case "DBNull":
				case "Exception":
				case "Type":
					Delayed.Add (ErrorHelper.CreateWarning (1011, $"Type `{t}` is not generated because it lacks a native counterpart."));
					unsupported.Add (t);
					return false;
				case "Decimal":
					return AddDecimalSupport (t);
				case "DateTime":
					return AddDateTimeSupport (t);
				case "TimeSpan":
					if (system_timespan == null) {
						system_timespan = new ProcessedType (t) {
							Assembly = GetMscorlib (t),
						};
						AddExtraType (system_timespan);
					}
					return true;
				case "IFormatProvider":
					if (system_iformatprovider == null) {
						system_iformatprovider = new ProcessedType (t) {
							Assembly = GetMscorlib (t),
						};
						AddExtraType (system_iformatprovider);
					}
					return true;
				}
				break;
			case "System.Globalization":
				switch (t.Name) {
				case "TimeSpanStyles": // enum for TimeSpan support
					if (system_globalization_timespanstyles == null) {
						system_globalization_timespanstyles = new ProcessedType (t) {
							Assembly = GetMscorlib (t),
						};
						AddExtraType (system_globalization_timespanstyles);
					}
					return true;
				}
				break;
			}

			var base_type = t.BaseType;
			return (base_type == null) || base_type.Is ("System", "Object") ? true : IsSupported (base_type);
		}

		protected override IEnumerable<Type> GetTypes (Assembly a)
		{
			foreach (var t in a.GetTypes ()) {
				if (!t.IsPublic)
					continue;

				if (!IsSupported (t))
					continue;

				if (IsNSObjectSubclass (t))
					continue; // The static registrar generates the code for these types.

				yield return t;
			}
		}

		protected IEnumerable<ProcessedConstructor> GetConstructors (ProcessedType processedType)
		{
			Type t = processedType.Type;

			foreach (var ctor in t.GetConstructors ()) {
				// .cctor not to be called directly by native code
				if (ctor.IsStatic)
					continue;
				if (!ctor.IsPublic)
					continue;

				bool pcheck = true;
				foreach (var p in ctor.GetParameters ()) {
					var pt = p.ParameterType;
					if (!IsSupported (pt)) {
						Delayed.Add (ErrorHelper.CreateWarning (1020, $"Constructor `{ctor}` is not generated because of parameter type `{pt}` is not supported."));
						pcheck = false;
					} else if (p.HasDefaultValue) {
						members_with_default_values.Add (ctor);
					}
				}
				if (!pcheck)
					continue;

				yield return new ProcessedConstructor (ctor, this, processedType);
			}
		}

		internal Dictionary<Type,MethodInfo> icomparable = new Dictionary<Type, MethodInfo> ();
		HashSet<MemberInfo> members_with_default_values = new HashSet<MemberInfo> ();

		// defining type / extended type / methods
		internal Dictionary<Type, Dictionary<Type, List<ProcessedMethod>>> extensions_methods = new Dictionary<Type, Dictionary<Type, List<ProcessedMethod>>> ();

		protected IEnumerable<ProcessedMethod> GetMethods (ProcessedType type)
		{
			var t = type.Type;
			foreach (var mi in t.GetMethods (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)) {
				if (!mi.IsPublic)
					continue;

				// handle special cases where we can implement something better, e.g. a better match
				if (implement_system_icomparable_t) {
					// for X we prefer `IComparable<X>` to `IComparable` - since it will be exposed identically to ObjC
					if (mi.Match ("System.Int32", "CompareTo", t.FullName)) {
						icomparable [t] = mi;
						continue;
					}
				}
				if (implement_system_icomparable && mi.Match ("System.Int32", "CompareTo", "System.Object")) {
					// don't replace CompareTo(T) with CompareTo(Object)
					if (!icomparable.ContainsKey (t))
						icomparable.Add (t, mi);
					continue;
				}

				if (mi.Match ("System.Boolean", "Equals", "System.Object")) {
					yield return new ProcessedMethod (mi, this, type) {
						DeclaringType = type,
						MethodType = MethodType.NSObjectProcotolIsEqual,
					};
					continue;
				} 

				if (implement_system_iequatable_t && mi.Match ("System.Boolean", "Equals", new string [] { null })) {
					yield return new ProcessedMethod (mi, this, type) {
						DeclaringType = type,
						MethodType = MethodType.IEquatable,
					};
					continue;
				}

				if (mi.Match ("System.Int32", "GetHashCode")) {
					yield return new ProcessedMethod (mi, this, type) {
						DeclaringType = type,
						MethodType = MethodType.NSObjectProcotolHash,
					};
					continue;
				}

				var rt = mi.ReturnType;
				if (!IsSupported (rt)) {
					Delayed.Add (ErrorHelper.CreateWarning (1030, $"Method `{mi}` is not generated because return type `{rt}` is not supported."));
					continue;
				}

				bool pcheck = true;
				foreach (var p in mi.GetParameters ()) {
					var pt = p.ParameterType;
					if (!IsSupported (pt)) {
						Delayed.Add (ErrorHelper.CreateWarning (1031, $"Method `{mi}` is not generated because of parameter type `{pt}` is not supported."));
						pcheck = false;
					} else if (p.HasDefaultValue) {
						members_with_default_values.Add (mi);
					}
				}
				if (!pcheck)
					continue;

				// handle extension methods
				if (extension_type && mi.HasCustomAttribute ("System.Runtime.CompilerServices", "ExtensionAttribute")) {
					var extended_type = mi.GetParameters () [0].ParameterType;
					if (extended_type.IsPrimitive) {
						Delayed.Add (ErrorHelper.CreateWarning (1034, $"Extension method `{mi}` is not generated inside a category because they cannot be created on primitive type `{extended_type}`. A normal, static method was generated."));
					} else {
						Dictionary<Type, List<ProcessedMethod>> extensions;
						if (!extensions_methods.TryGetValue (t, out extensions)) {
							extensions = new Dictionary<Type, List<ProcessedMethod>> ();
							extensions_methods.Add (t, extensions);
						}
						List<ProcessedMethod> extmethods;
						if (!extensions.TryGetValue (extended_type, out extmethods)) {
							extmethods = new List<ProcessedMethod> ();
							extensions.Add (extended_type, extmethods);
						}
						extmethods.Add (new ProcessedMethod  (mi, this, type) { IsExtension = true } );
						continue;
					}
				}

				yield return new ProcessedMethod (mi, this, type);
			}
		}

		protected IEnumerable<PropertyInfo> GetProperties (Type t)
		{
			foreach (var pi in t.GetProperties (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)) {
				var pt = pi.PropertyType;
				if (!IsSupported (pt)) {
					Delayed.Add (ErrorHelper.CreateWarning (1040, $"Property `{pi}` is not generated because of parameter type `{pt}` is not supported."));
					continue;
				}
				yield return pi;
			}
		}

		protected IEnumerable<ProcessedFieldInfo> GetFields (ProcessedType t)
		{
			foreach (var fi in t.Type.GetFields (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)) {
				if (!fi.IsPublic)
					continue;
				var ft = fi.FieldType;
				if (!IsSupported (ft)) {
					Delayed.Add (ErrorHelper.CreateWarning (1050, $"Field `{fi}` is not generated because of field type `{ft}` is not supported."));
					continue;
				}
				yield return new ProcessedFieldInfo (fi, this, t);
			}
		}

		internal Dictionary<Type, List<ProcessedProperty>> subscriptProperties = new Dictionary<Type, List<ProcessedProperty>> ();

		// special cases
		bool implement_system_icomparable;
		bool implement_system_icomparable_t;
		bool implement_system_iequatable_t;
		bool extension_type;


		public override void Process (IEnumerable<ProcessedAssembly> input)
		{
			base.Process (input);

			Types = Types.OrderBy ((arg) => arg.Type.FullName).OrderBy ((arg) => Types.HasClass (arg.Type.BaseType)).ToList ();
			Console.WriteLine ($"\t{Types.Count} types found");

			// proceed with extra adjustments before giving results to the generator
			foreach (var t in Types) {
				foreach (var uctor in GetUnavailableParentCtors (t)) {
					var c = new ProcessedConstructor (uctor.Constructor, this, t) { Unavailable = true };
					t.Constructors.Add (c);
				}
			}

			// we want to create wrappers only if the signature does not already exists, e.g. both `.ctor ()` and `.ctor (int i = 0)` can co-exists in .net
			foreach (var dv in members_with_default_values) {
				var pt = GetProcessedType (dv.DeclaringType);
				var ci = dv as ConstructorInfo;
				if (ci != null) {
					foreach (var pc in AddDefaultValuesWrappers (ci, pt)) {
						if (!pt.SignatureExists (pc))
							pt.Constructors.Add (pc);
						else
							Delayed.Add (ErrorHelper.CreateWarning (1021, $"Constructor `{ci}` has default values for which no wrapper is generated."));
					}
					continue;
				}
				var mi = dv as MethodInfo;
				foreach (var pm in AddDefaultValuesWrappers (mi, pt)) {
					if (!pt.SignatureExists (pm))
						pt.Methods.Add (pm);
					else
						Delayed.Add (ErrorHelper.CreateWarning (1032, $"Method `{mi}` has default values for which no wrapper is generated."));
				}
			}

			ErrorHelper.Show (Delayed);
		}

		public override void Process (ProcessedType pt)
		{
			Types.Add (pt);
			if (pt.IsNativeReference)
				return;

			var t = pt.Type;
			if (t.IsEnum)
				return;

			extension_type = t.HasCustomAttribute ("System.Runtime.CompilerServices", "ExtensionAttribute");

			implement_system_icomparable = t.Implements ("System", "IComparable");
			implement_system_icomparable_t = t.Implements("System", "IComparable`1");
			implement_system_iequatable_t = t.Implements ("System", "IEquatable`1");

			var constructors = GetConstructors (pt).OrderBy ((arg) => arg.Constructor.ParameterCount).ToList ();
			var processedConstructors = PostProcessConstructors (constructors).ToList ();
			pt.Constructors = processedConstructors;

			var meths = GetMethods (pt).OrderBy ((arg) => arg.Method.Name).ToList ();
			var processedMethods = PostProcessMethods (meths).ToList ();
			pt.Methods = processedMethods;

			var props = new List<ProcessedProperty> ();
			var subscriptProps = new List<ProcessedProperty> ();
			foreach (var pi in GetProperties (t)) {
				var getter = pi.GetGetMethod ();
				var setter = pi.GetSetMethod ();
				// setter only property are valid in .NET and we need to generate a method in ObjC (there's no writeonly properties)
				if (getter == null)
					continue;

				// indexers are implemented as methods and object subscripting
				if ((getter.ParameterCount > 0) || ((setter != null) && setter.ParameterCount > 1)) {
					subscriptProps.Add (new ProcessedProperty (pi, this, pt));
					continue;
				}

				// we can do better than methods for the more common cases (readonly and readwrite)
				processedMethods.RemoveAll (x => x.Method == getter);
				processedMethods.RemoveAll (x => x.Method == setter);
				props.Add (new ProcessedProperty (pi, this, pt));
			}
			props = props.OrderBy ((arg) => arg.Name).ToList ();
			var processedProperties = PostProcessProperties (props).ToList ();
			pt.Properties = processedProperties;

			if (subscriptProps.Count > 0) {
				if (subscriptProps.Count > 1)
					Delayed.Add (ErrorHelper.CreateWarning (1041, $"Indexed properties on {t.Name} is not generated because multiple indexed properties not supported."));
				else
					subscriptProperties.Add (t, PostProcessSubscriptProperties (subscriptProps).ToList ());
			}

			// fields will need to be wrapped within properties
			var f = GetFields (pt).OrderBy ((arg) => arg.Name).ToList ();
			var processedFields = PostProcessFields (f).ToList ();
			pt.Fields = processedFields;
		}

		// post processing logic

		IEnumerable<ProcessedConstructor> GetUnavailableParentCtors (ProcessedType pt)
		{
			var type = pt.Type;
			var baseType = type.BaseType;
			if ((baseType == null) || (baseType.Namespace == "System" && baseType.Name == "Object"))
				return Enumerable.Empty<ProcessedConstructor> ();

			var baseProcessedType = GetProcessedType (baseType);
			if ((baseProcessedType == null) || !baseProcessedType.HasConstructors)
				return Enumerable.Empty<ProcessedConstructor> ();
			
			var typeCtors = pt.Constructors;
			List<ProcessedConstructor> parentCtors = baseProcessedType.Constructors;

			var finalList = new List<ProcessedConstructor> ();
			foreach (var pctor in parentCtors) {
				var pctorParams = pctor.Parameters;
				foreach (var ctor in typeCtors) {
					var ctorParams = ctor.Parameters;
					if (pctorParams.Any (pc => !ctorParams.Any (p => p.Position == pc.Position && pc.ParameterType == p.ParameterType))) {
						finalList.Add (pctor);
						break;
					}
				}
			}

			return finalList;
		}

		IEnumerable<ProcessedConstructor> AddDefaultValuesWrappers (ConstructorInfo ci, ProcessedType containingType)
		{
			// parameters with default values must be at the end and there can be many of them
			var parameters = ci.GetParameters ();
			for (int i = parameters.Length - 1; i >= 0; i--) {
				if (!parameters [i].HasDefaultValue)
					continue;
				var pc = new ProcessedConstructor (ci, this, containingType) {
					ConstructorType = ConstructorType.DefaultValueWrapper,
					FirstDefaultParameter = i,
				};
				yield return pc;
			}
		}

		IEnumerable<ProcessedMethod> AddDefaultValuesWrappers (MethodInfo mi, ProcessedType containingType)
		{
			// parameters with default values must be at the end and there can be many of them
			var parameters = mi.GetParameters ();
			for (int i = parameters.Length - 1; i >= 0; i--) {
				if (!parameters [i].HasDefaultValue)
					continue;
				var pm = new ProcessedMethod (mi, this, containingType) {
					MethodType = MethodType.DefaultValueWrapper,
					FirstDefaultParameter = i,
				};
				yield return pm;
			}
		}
	}
}
