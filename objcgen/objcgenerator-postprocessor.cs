using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;
using Embeddinator;
using System.Globalization;

namespace ObjC {
	// A set of post-processing steps needed to add hints
	// to the input of the generation step
	public partial class ObjCProcessor {

		protected IEnumerable<ProcessedMethod> PostProcessMethods (IEnumerable<ProcessedMethod> methods)
		{
			HashSet<string> duplicateNames = FindDuplicateNames (methods);

			var equals = new HashSet<MethodInfo> ();
			foreach (var m in methods) {
				if (m.MethodType == MethodType.NSObjectProcotolIsEqual)
					equals.Add (m.Method);
			}
			HashSet<MethodInfo> operatorToIgnore = new HashSet<MethodInfo> (OperatorOverloads.FindOperatorPairToIgnore (methods, equals));

			foreach (var processedMethod in methods) {
				var method = processedMethod.Method;
				if (operatorToIgnore.Contains (method)) {
					Delayed.Add (ErrorHelper.CreateWarning (1033, $"Method {method.Name} is not generated because another method exposes the operator with a friendly name"));
					continue;
				}

				if (duplicateNames.Contains (CreateStringRep (method)) && method.Name != "CompareTo") // HACK
					processedMethod.FallBackToTypeName = true;

				if (IsOperatorOrFriendlyVersion (method))
					processedMethod.IsOperator = true;

				ProcessPotentialName (processedMethod);

				yield return processedMethod;
			}
		}

		void ProcessPotentialName (ProcessedMethod processedMethod)
		{
			MethodInfo method = processedMethod.Method;
			if (IsOperatorOrFriendlyVersion (method)) {
				string nameOverride = OperatorOverloads.GetObjCName (processedMethod.Method.Name, processedMethod.Method.ParameterCount);
				if (nameOverride != null)
					processedMethod.NameOverride = nameOverride;
			}

			string objCSignature = processedMethod.ObjCSignature;
			if (RestrictedObjSelectors.IsImportantSelector (objCSignature)) {
				string newName = "managed" + method.Name.PascalCase ();
				processedMethod.NameOverride = newName;
				Delayed.Add (ErrorHelper.CreateWarning (1051, $"Element {method.Name} is generated instead as {newName} because its name conflicts with an important objective-c selector."));
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

				ProcessPotentialName (processedProperty);

				yield return processedProperty;
			}
		}

		void ProcessPotentialName (ProcessedProperty processedProperty)
		{
			string getSignature = processedProperty.HasGetter ? processedProperty.GetMethod.ObjCSignature : "";
			string setSignature = processedProperty.HasSetter ? processedProperty.SetMethod.ObjCSignature : "";

			if (RestrictedObjSelectors.IsImportantSelector (getSignature) || RestrictedObjSelectors.IsImportantSelector (setSignature)) {
				string newName = "managed" + processedProperty.Name.PascalCase ();
				Delayed.Add (ErrorHelper.CreateWarning (1051, $"Element {processedProperty.Name} is generated instead as {newName} because its name conflicts with an important objective-c selector."));
				processedProperty.NameOverride = newName;
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

		// temporary quasi-duplicate
		static HashSet<string> FindDuplicateNames (IEnumerable<ProcessedMethod> members)
		{
			Dictionary<string, int> methodNames = new Dictionary<string, int> ();
			foreach (var member in members)
				methodNames.IncrementValue (CreateStringRep (member.Method));
			return new HashSet<string> (methodNames.Where (x => x.Value > 1).Select (x => x.Key));
		}

		static HashSet<string> FindDuplicateNames (IEnumerable<MemberInfo> members)
		{
			Dictionary<string, int> methodNames = new Dictionary<string, int> ();
			foreach (MemberInfo member in members)
				methodNames.IncrementValue (CreateStringRep (member));
			return new HashSet<string> (methodNames.Where (x => x.Value > 1).Select (x => x.Key));
		}
	}

	static class RestrictedObjSelectors
	{
		static readonly HashSet<string> ImportantObjcSelectors = new HashSet<string> { "hash", "class", "superclass", "isEqual:", "self", "isKindOfClass:",
			"isMemberOfClass:", "respondsToSelector:", "conformsToProtocol:", "description", "debugDescription", "performSelector:", "performSelector:withObject:",
			"performSelector:withObject:withObject:", "isProxy", "retain", "release", "autorelease", "retainCount", "zone" };

		static public bool IsImportantSelector (string selector)
		{
			if (selector.StartsWith ("get", StringComparison.Ordinal))
				selector = selector.Substring (3).CamelCase ();

			if (selector.StartsWith ("set", StringComparison.Ordinal)) {
				selector = selector.Substring (3).CamelCase ();
				int colonLocation = selector.IndexOf (':');
				if (colonLocation > 0)
					selector = selector.Substring (0, colonLocation);
			}

			return ImportantObjcSelectors.Contains (selector);
		}
	}

}
