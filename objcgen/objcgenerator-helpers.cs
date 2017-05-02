using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

using Embeddinator;
namespace ObjC {
	public partial class ObjCGenerator {

		// get a name that is safe to use from ObjC code

		void GetSignatures (string objName, string monoName, MemberInfo info, ParameterInfo [] parameters, bool useTypeNames, bool isExtension, out string objcSignature, out string monoSignature)
		{
			var method = (info as MethodBase); // else it's a PropertyInfo
			// special case for setter-only - the underscore looks ugly
			if ((method != null) && method.IsSpecialName)
				objName = objName.Replace ("_", String.Empty);

			var objc = new StringBuilder (objName);
			var mono = new StringBuilder (monoName);

			mono.Append ('(');

			for (int n = 0; n < parameters.Length; ++n) {
				ParameterInfo p = parameters [n];

				if (objc.Length > objName.Length) {
					objc.Append (' ');
					mono.Append (',');
				}

				string paramName = useTypeNames ? p.ParameterType.Name : p.Name;
				if ((method != null) && (n > 0 || !isExtension)) {
					if (n == 0) {
						bool mutatePropertyOrOperatorMethod = useTypeNames && (method.IsPropertyMethod () || method.IsOperatorMethod ());
						if (method.IsConstructor || mutatePropertyOrOperatorMethod || !method.IsSpecialName)
							objc.Append (paramName.PascalCase ());
					} else
						objc.Append (paramName.ToLowerInvariant ());
				}

				if (n > 0 || !isExtension) {
					string ptname = NameGenerator.GetObjCParamName (p.ParameterType, types);
					objc.Append (":(").Append (ptname).Append (")").Append (p.Name);
				}
				mono.Append (NameGenerator.GetMonoName (p.ParameterType));
			}

			mono.Append (')');

			objcSignature = objc.ToString ();
			monoSignature = mono.ToString ();
		}

		public IEnumerable<ProcessedConstructor> GetUnavailableParentCtors (Type type, List<ProcessedConstructor> typeCtors)
		{
			var baseType = type.BaseType;
			if (baseType.Namespace == "System" && baseType.Name == "Object")
				return Enumerable.Empty<ProcessedConstructor> ();

			List<ProcessedConstructor> parentCtors;
			if (!ctors.TryGetValue (baseType, out parentCtors))
				return Enumerable.Empty<ProcessedConstructor> ();

			var finalList = new List<ProcessedConstructor> ();
			foreach (var pctor in parentCtors) {
				var pctorParams = pctor.Constructor.GetParameters ();
				foreach (var ctor in typeCtors) {
					var ctorParams = ctor.Constructor.GetParameters ();
					if (pctorParams.Any (pc => !ctorParams.Any (p => p.Position == pc.Position && pc.ParameterType == p.ParameterType))) {
						finalList.Add (pctor);
						break;
					}
				}
			}

			return finalList;
		}
	}
}
