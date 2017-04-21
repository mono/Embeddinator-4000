using System;
using System.Collections.Generic;
using System.Linq;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

using Embeddinator;
using System.Text;

namespace ObjC {
	// A set of post-processing steps needed to add hints
	// to the input of the generation step
	public partial class ObjCGenerator {
		protected IEnumerable<ProcessedMethod> PostProcessMethods (IEnumerable<MethodInfo> methods)
		{
			HashSet<string> duplicateNames = FindDuplicateNames (methods);

			foreach (MethodInfo method in methods) {
				ProcessedMethod processedMethod = new ProcessedMethod (method);

				if (duplicateNames.Contains (method.Name))
					processedMethod.FallBackToTypeName = true;

				yield return processedMethod;
			}
		}

		protected IEnumerable<ProcessedProperty> PostProcessProperties (IEnumerable<PropertyInfo> properties)
		{
			foreach (PropertyInfo property in properties) {
				ProcessedProperty processedProperty = new ProcessedProperty (property);
				yield return processedProperty;
			}
		}

		protected IEnumerable<ProcessedProperty> PostProcessSubscriptProperties (IEnumerable<PropertyInfo> properties)
		{
			foreach (PropertyInfo property in properties) {
				ProcessedProperty processedProperty = new ProcessedProperty (property);
				yield return processedProperty;
			}
		}

		protected IEnumerable<ProcessedFieldInfo> PostProcessFields (IEnumerable<FieldInfo> fields)
		{
			foreach (FieldInfo field in fields) {
				ProcessedFieldInfo processedField = new ProcessedFieldInfo (field);
				yield return processedField;
			}
		}

		protected IEnumerable<ProcessedConstructor> PostProcessConstructors (IEnumerable<ConstructorInfo> constructors)
		{
			HashSet<string> duplicateNames = FindDuplicateConstructors (constructors);

			foreach (ConstructorInfo constructor in constructors) {
				ProcessedConstructor processedConstructor = new ProcessedConstructor (constructor);

				if (duplicateNames.Contains (CreateStringRepOfConstructor(constructor)))
					processedConstructor.FallBackToTypeName = true;

				yield return processedConstructor;
			}
		}

		static string CreateStringRepOfConstructor (ConstructorInfo constructor)
		{
			StringBuilder str = new StringBuilder ();
			foreach (var arg in constructor.GetParameters ())
				str.Append (arg.Name + ":"); // This format is arbitrary
			return str.ToString ();
		}

		static HashSet<string> FindDuplicateConstructors (IEnumerable<ConstructorInfo> constructors)
		{
			Dictionary<string, int> methodNames = new Dictionary<string, int> ();
			foreach (ConstructorInfo constructor in constructors) {
				string ctorString = CreateStringRepOfConstructor (constructor);
				if (methodNames.ContainsKey (ctorString))
					methodNames[ctorString]++;
				else
					methodNames[ctorString] = 1;
			}
			return new HashSet<string> (methodNames.Where (x => x.Value > 1).Select (x => x.Key));
		}

		static HashSet<string> FindDuplicateNames (IEnumerable<MemberInfo> members)
		{
			Dictionary<string, int> methodNames = new Dictionary<string, int> ();
			foreach (MemberInfo member in members) {
				if (methodNames.ContainsKey (member.Name))
					methodNames[member.Name]++;
				else
					methodNames[member.Name] = 1;
			}
			return new HashSet<string> (methodNames.Where (x => x.Value > 1).Select (x => x.Key));
		}
	}
}