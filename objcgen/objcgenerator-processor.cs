using System;
using System.Collections.Generic;
using System.Linq;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

using Embeddinator;

namespace ObjC {

	public partial class ObjCGenerator {

		List<Exception> delayed = new List<Exception> ();
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

		bool IsSupported (Type t)
		{
			if (t.IsByRef)
				return IsSupported (t.GetElementType ());

			if (unsupported.Contains (t))
				return false;

			if (t.IsArray) {
				delayed.Add (ErrorHelper.CreateWarning (1010, $"Type `{t}` is not generated because `arrays` are not supported."));
				unsupported.Add (t);
				return false;
			}

			if (t.IsPointer) {
				delayed.Add (ErrorHelper.CreateWarning (1010, $"Type `{t}` is not generated because `unsafe pointers` are not supported."));
				unsupported.Add (t);
				return false;
			}

			if (t.IsGenericParameter || t.IsGenericType) {
				delayed.Add (ErrorHelper.CreateWarning (1010, $"Type `{t}` is not generated because `generics` are not supported."));
				unsupported.Add (t);
				return false;
			}

			switch (t.Namespace) {
			case "System":
				switch (t.Name) {
				case "Object": // we cannot accept arbitrary NSObject (which we might not have bound) into mono
				case "DBNull":
				case "Exception":
				case "IFormatProvider":
				case "Type":
					delayed.Add (ErrorHelper.CreateWarning (1011, $"Type `{t}` is not generated because it lacks a native counterpart."));
					unsupported.Add (t);
					return false;
				case "DateTime": // FIXME: NSDateTime
				case "Decimal": // FIXME: NSDecimal
				case "TimeSpan":
					delayed.Add (ErrorHelper.CreateWarning (1012, $"Type `{t}` is not generated because it lacks a marshaling code with a native counterpart."));
					unsupported.Add (t);
					return false;
				}
				break;
			}

			var base_type = t.BaseType;
			return (base_type == null) || base_type.Is ("System", "Object") ? true : IsSupported (base_type);
		}

		protected IEnumerable<Type> GetTypes (Assembly a)
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

		protected IEnumerable<ConstructorInfo> GetConstructors (Type t)
		{
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
						delayed.Add (ErrorHelper.CreateWarning (1020, $"Constructor `{ctor}` is not generated because of parameter type `{pt}` is not supported."));
						pcheck = false;
					} else if (p.HasDefaultValue) {
						members_with_default_values.Add (ctor);
					}
				}
				if (!pcheck)
					continue;

				yield return ctor;
			}
		}

		Dictionary<Type,MethodInfo> icomparable = new Dictionary<Type, MethodInfo> ();
		Dictionary<Type, MethodInfo> equals = new Dictionary<Type, MethodInfo> ();
		Dictionary<Type, MethodInfo> hashes = new Dictionary<Type, MethodInfo> ();
		HashSet<MemberInfo> members_with_default_values = new HashSet<MemberInfo> ();

		// defining type / extended type / methods
		Dictionary<Type, Dictionary<Type, List<MethodInfo>>> extensions_methods = new Dictionary<Type, Dictionary<Type, List<MethodInfo>>> ();

		protected IEnumerable<MethodInfo> GetMethods (Type t)
		{
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
					equals.Add (t, mi);
					continue;
				}

				if (mi.Match ("System.Int32", "GetHashCode")) {
					hashes.Add (t, mi);
					continue;
				}

				var rt = mi.ReturnType;
				if (!IsSupported (rt)) {
					delayed.Add (ErrorHelper.CreateWarning (1030, $"Method `{mi}` is not generated because return type `{rt}` is not supported."));
					continue;
				}

				bool pcheck = true;
				foreach (var p in mi.GetParameters ()) {
					var pt = p.ParameterType;
					if (!IsSupported (pt)) {
						delayed.Add (ErrorHelper.CreateWarning (1031, $"Method `{mi}` is not generated because of parameter type `{pt}` is not supported."));
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
						delayed.Add (ErrorHelper.CreateWarning (1034, $"Extension method `{mi}` is not generated inside a category because they cannot be created on primitive type `{extended_type}`. A normal, static method was generated."));
					} else {
						Dictionary<Type, List<MethodInfo>> extensions;
						if (!extensions_methods.TryGetValue (t, out extensions)) {
							extensions = new Dictionary<Type, List<MethodInfo>> ();
							extensions_methods.Add (t, extensions);
						}
						List<MethodInfo> extmethods;
						if (!extensions.TryGetValue (extended_type, out extmethods)) {
							extmethods = new List<MethodInfo> ();
							extensions.Add (extended_type, extmethods);
						}
						extmethods.Add (mi);
						continue;
					}
				}

				yield return mi;
			}
		}

		protected IEnumerable<PropertyInfo> GetProperties (Type t)
		{
			foreach (var pi in t.GetProperties (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)) {
				var pt = pi.PropertyType;
				if (!IsSupported (pt)) {
					delayed.Add (ErrorHelper.CreateWarning (1040, $"Property `{pi}` is not generated because of parameter type `{pt}` is not supported."));
					continue;
				}
				yield return pi;
			}
		}

		protected IEnumerable<FieldInfo> GetFields (Type t)
		{
			foreach (var fi in t.GetFields (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)) {
				if (!fi.IsPublic)
					continue;
				var ft = fi.FieldType;
				if (!IsSupported (ft)) {
					delayed.Add (ErrorHelper.CreateWarning (1050, $"Field `{fi}` is not generated because of field type `{ft}` is not supported."));
					continue;
				}
				yield return fi;
			}
		}

		List<ProcessedType> types = new List<ProcessedType> ();

		Dictionary<Type, List<ProcessedConstructor>> ctors = new Dictionary<Type, List<ProcessedConstructor>> ();
		Dictionary<Type, List<ProcessedMethod>> methods = new Dictionary<Type, List<ProcessedMethod>> ();
		Dictionary<Type, List<ProcessedProperty>> properties = new Dictionary<Type, List<ProcessedProperty>> ();
		Dictionary<Type, List<ProcessedFieldInfo>> fields = new Dictionary<Type, List<ProcessedFieldInfo>> ();
		Dictionary<Type, List<ProcessedProperty>> subscriptProperties = new Dictionary<Type, List<ProcessedProperty>> ();

		// special cases
		bool implement_system_icomparable;
		bool implement_system_icomparable_t;
		bool extension_type;

		Queue<ProcessedAssembly> AssemblyQueue;

		public override void Process (IEnumerable<Assembly> input)
		{
			AssemblyQueue = new Queue<ProcessedAssembly> ();
			foreach (var a in input) {
				var pa = new ProcessedAssembly (a) {
					UserCode = true,
				};
				// ignoring/warning one is not an option as they could be different (e.g. different builds/versions)
				if (!AddIfUnique (pa))
					throw ErrorHelper.CreateError (12, $"The assembly name `{pa.Name}` is not unique");
				AssemblyQueue.Enqueue (pa);
			}

			while (AssemblyQueue.Count > 0) {
				Process (AssemblyQueue.Dequeue ());
			}

			// we can add new types while processing some (e.g. categories)
			var typeQueue = new Queue<ProcessedType> (types);
			types.Clear (); // reuse
			while (typeQueue.Count > 0) {
		                Process (typeQueue.Dequeue ());
			}

			types = types.OrderBy ((arg) => arg.Type.FullName).OrderBy ((arg) => types.HasClass (arg.Type.BaseType)).ToList ();
			Console.WriteLine ($"\t{types.Count} types found");

			ErrorHelper.Show (delayed);
		}

		List<ProcessedAssembly> Assemblies = new List<ProcessedAssembly> ();

		void Process (ProcessedAssembly a)
		{
			Assemblies.Add (a);
			if (!a.UserCode)
				return;

			foreach (var t in GetTypes (a.Assembly)) {
				var pt = new ProcessedType (t) {
					Assembly = a,
				};
				types.Add (pt);
			}
		}

		void Process (ProcessedType pt)
		{
			var t = pt.Type;

			types.Add (pt);
			if (t.IsEnum)
				return;

			extension_type = t.HasCustomAttribute ("System.Runtime.CompilerServices", "ExtensionAttribute");

			implement_system_icomparable = t.Implements ("System", "IComparable");
			implement_system_icomparable_t = t.Implements("System", "IComparable`1");

			var constructors = GetConstructors (t).OrderBy ((arg) => arg.ParameterCount).ToList ();
			var processedConstructors = PostProcessConstructors (constructors).ToList ();
			if (processedConstructors.Count > 0)
				ctors.Add (t, processedConstructors);

			var typeEquals = equals.Where (x => x.Key == t).Select (x => x.Value);
			var meths = GetMethods (t).OrderBy ((arg) => arg.Name).ToList ();
			var processedMethods = PostProcessMethods (meths, typeEquals).ToList ();
			if (processedMethods.Count > 0)
				methods.Add (t, processedMethods);

			var props = new List<PropertyInfo> ();
			var subscriptProps = new List<PropertyInfo> ();
			foreach (var pi in GetProperties (t)) {
				var getter = pi.GetGetMethod ();
				var setter = pi.GetSetMethod ();
				// setter only property are valid in .NET and we need to generate a method in ObjC (there's no writeonly properties)
				if (getter == null)
					continue;

				// indexers are implemented as methods and object subscripting
				if ((getter.ParameterCount > 0) || ((setter != null) && setter.ParameterCount > 1)) {
					subscriptProps.Add (pi);
					continue;
				}

				// we can do better than methods for the more common cases (readonly and readwrite)
				processedMethods.RemoveAll (x => x.Method == getter);
				processedMethods.RemoveAll (x => x.Method == setter);
				props.Add (pi);
			}
			props = props.OrderBy ((arg) => arg.Name).ToList ();
			var processedProperties = PostProcessProperties (props).ToList ();
			if (processedProperties.Count > 0)
				properties.Add (t, processedProperties);

			if (subscriptProps.Count > 0) {
				if (subscriptProps.Count > 1)
					delayed.Add (ErrorHelper.CreateWarning (1041, $"Indexed properties on {t.Name} is not generated because multiple indexed properties not supported."));
				else
					subscriptProperties.Add (t, PostProcessSubscriptProperties (subscriptProps).ToList ());
			}

			// fields will need to be wrapped within properties
			var f = GetFields (t).OrderBy ((arg) => arg.Name).ToList ();
			var processedFields = PostProcessFields (f).ToList ();
			if (processedFields.Count > 0)
				fields.Add (t, processedFields);
		}
	}
}
