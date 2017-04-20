using System;
using System.Collections.Generic;
using System.Linq;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

using Embeddinator;

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
				ProcessedProperty processedMethod = new ProcessedProperty (property);
				yield return processedMethod;
			}
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