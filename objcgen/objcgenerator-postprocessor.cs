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

		TypeMapper Mapper = new TypeMapper ();

		protected IEnumerable<ProcessedMethod> PostProcessMethods (IEnumerable<ProcessedMethod> methods)
		{
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

				if (IsOperatorOrFriendlyVersion (method))
					processedMethod.IsOperator = true;

				ProcessPotentialName (processedMethod);

				Mapper.CheckForDuplicateSelectors (processedMethod);

				Mapper.Register (processedMethod);

				processedMethod.Freeze ();
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

		protected IEnumerable<ProcessedProperty> PostProcessProperties (IEnumerable<ProcessedProperty> properties)
		{
			foreach (ProcessedProperty processedProperty in properties) {

				ProcessPotentialName (processedProperty);

				Mapper.CheckForDuplicateSelectors (processedProperty);

				Mapper.Register (processedProperty);

				processedProperty.Freeze ();
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

		protected IEnumerable<ProcessedProperty> PostProcessSubscriptProperties (IEnumerable<ProcessedProperty> properties)
		{
			foreach (ProcessedProperty processedProperty in properties) {

				Mapper.CheckForDuplicateSelectors (processedProperty);

				Mapper.Register (processedProperty);

				yield return processedProperty;
			}
		}

		void ProcessPotentialName (ProcessedFieldInfo processedField)
		{
			if (RestrictedObjSelectors.IsImportantSelector (processedField.GetterName) || RestrictedObjSelectors.IsImportantSelector (processedField.SetterName)) {
				string newName = "managed" + processedField.Name.PascalCase ();
				Delayed.Add (ErrorHelper.CreateWarning (1051, $"Element {processedField.Name} is generated instead as {newName} because its name conflicts with an important objective-c selector."));
				processedField.NameOverride = newName;
			}
		}

		protected IEnumerable<ProcessedFieldInfo> PostProcessFields (IEnumerable<ProcessedFieldInfo> fields)
		{
			foreach (ProcessedFieldInfo processedField in fields) {
				ProcessPotentialName (processedField);

				Mapper.CheckForDuplicateSelectors (processedField);

				Mapper.Register (processedField);

				yield return processedField;
			}
		}

		protected IEnumerable<ProcessedConstructor> PostProcessConstructors (IEnumerable<ProcessedConstructor> constructors)
		{
			foreach (ProcessedConstructor processedConstructor in constructors) {				
				Mapper.CheckForDuplicateSelectors (processedConstructor);

				Mapper.Register (processedConstructor);

				processedConstructor.Freeze ();
				yield return processedConstructor;
			}
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
