using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;
using Embeddinator;

namespace ObjC {
	// A set of post-processing steps needed to add hints
	// to the input of the generation step
	public partial class ObjCProcessor {
		protected IEnumerable<ProcessedMethod> PostProcessMethods (IEnumerable<MethodInfo> methods, IEnumerable <MethodInfo> equals)
		{
			HashSet<string> duplicateNames = FindDuplicateNames (methods);
			HashSet<MethodInfo> operatorToIgnore = new HashSet<MethodInfo> (OperatorOverloads.FindOperatorPairToIgnore (methods, equals));

			foreach (MethodInfo method in methods) {
				if (operatorToIgnore.Contains (method)) {
					Delayed.Add (ErrorHelper.CreateWarning (1033, $"Method {method.Name} is not generated because another method exposes the operator with a friendly name"));
					continue;
				}

				ProcessedMethod processedMethod = new ProcessedMethod (method, this);

				if (duplicateNames.Contains (CreateStringRep (method)) && method.Name != "CompareTo") // HACK
					processedMethod.FallBackToTypeName = true;

				if (IsOperatorOrFriendlyVersion (method))
					processedMethod.IsOperator = true;

				ProcessPotentialNameOverride (processedMethod);

				processedMethod.ComputeSignatures ();
				yield return processedMethod;
			}
		}

		void ProcessPotentialNameOverride (ProcessedMethod processedMethod)
		{
			MethodInfo method = processedMethod.Method;
			if (IsOperatorOrFriendlyVersion (method)) {
				string nameOverride = OperatorOverloads.GetObjCName (processedMethod.Method.Name, processedMethod.Method.ParameterCount);
				if (nameOverride != null)
					processedMethod.NameOverride = nameOverride;
			}
		}

		public bool IsOperatorOrFriendlyVersion (MethodInfo method)
		{
			return method.IsOperatorMethod () || OperatorOverloads.MatchesOperatorFriendlyName (method);
		}

		protected IEnumerable<ProcessedProperty> PostProcessProperties (IEnumerable<PropertyInfo> properties)
		{
			foreach (PropertyInfo property in properties) {
				ProcessedProperty processedProperty = new ProcessedProperty (property, this);
				yield return processedProperty;
			}
		}

		protected IEnumerable<ProcessedProperty> PostProcessSubscriptProperties (IEnumerable<PropertyInfo> properties)
		{
			foreach (PropertyInfo property in properties) {
				ProcessedProperty processedProperty = new ProcessedProperty (property, this);
				yield return processedProperty;
			}
		}

		protected IEnumerable<ProcessedFieldInfo> PostProcessFields (IEnumerable<FieldInfo> fields)
		{
			foreach (FieldInfo field in fields) {
				ProcessedFieldInfo processedField = new ProcessedFieldInfo (field, this);
				yield return processedField;
			}
		}

		protected IEnumerable<ProcessedConstructor> PostProcessConstructors (IEnumerable<ConstructorInfo> constructors)
		{
			HashSet<string> duplicateNames = FindDuplicateNames (constructors);

			foreach (ConstructorInfo constructor in constructors) {
				ProcessedConstructor processedConstructor = new ProcessedConstructor (constructor, this);

				if (duplicateNames.Contains (CreateStringRep(constructor)))
					processedConstructor.FallBackToTypeName = true;

				processedConstructor.ComputeSignatures ();
				yield return processedConstructor;
			}
		}

		static string CreateStringRep (ConstructorInfo constructor)
		{
			StringBuilder str = new StringBuilder ();
			foreach (var arg in constructor.GetParameters ())
				str.Append (arg.Name + ":"); // This format is arbitrary
			return str.ToString ();
		}

		static string CreateStringRep (MethodInfo method)
		{
			StringBuilder str = new StringBuilder (method.Name);
			foreach (var arg in method.GetParameters ())
				str.Append (":"); // This format is arbitrary
			return str.ToString ();
		}

		static string CreateStringRep (MemberInfo i)
		{
			if (i is ConstructorInfo)
				return CreateStringRep ((ConstructorInfo)i);
			if (i is MethodInfo)
				return CreateStringRep ((MethodInfo)i);
			return i.Name;
		}

		static HashSet<string> FindDuplicateNames (IEnumerable<MemberInfo> members)
		{
			Dictionary<string, int> methodNames = new Dictionary<string, int> ();
			foreach (MemberInfo member in members)
				methodNames.IncrementValue (CreateStringRep (member));
			return new HashSet<string> (methodNames.Where (x => x.Value > 1).Select (x => x.Key));
		}
	}
}
