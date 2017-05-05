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
					string ptname = NameGenerator.GetObjCParamTypeName (p, Processor.Types);
					objc.Append (":(").Append (ptname).Append (")").Append (NameGenerator.GetExtendedParameterName (p, parameters));
				}
				mono.Append (NameGenerator.GetMonoName (p.ParameterType));
			}

			mono.Append (')');

			objcSignature = objc.ToString ();
			monoSignature = mono.ToString ();
		}
	}
}
