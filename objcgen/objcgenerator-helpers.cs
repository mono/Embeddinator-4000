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
		public static string GetObjCName (Type t)
		{
			return t.FullName.Replace ('.', '_');
		}

		void GetSignatures (string objName, string monoName, MemberInfo info, ParameterInfo [] parameters, out string objcSignature, out string monoSignature)
		{
			var method = (info as MethodBase); // else it's a PropertyInfo
			// special case for setter-only - the underscore looks ugly
			if ((method != null) && method.IsSpecialName)
				objName = objName.Replace ("_", String.Empty);
			StringBuilder objc = new StringBuilder (objName);
			var mono = new StringBuilder (monoName);
			mono.Append ('(');
			int n = 0;
			foreach (var p in parameters) {
				if (objc.Length > objName.Length) {
					objc.Append (' ');
					mono.Append (',');
				}
				if (method != null) {
					if (n == 0) {
						if (method.IsConstructor || !method.IsSpecialName)
							objc.Append (PascalCase (p.Name));
					} else
						objc.Append (p.Name.ToLowerInvariant ());
				}
				var pt = p.ParameterType;
				var ptname = GetTypeName (p.ParameterType);
				if (types.Contains (pt))
					ptname += " *";
				objc.Append (":(").Append (ptname).Append (") ").Append (p.Name);
				mono.Append (GetMonoName (p.ParameterType));
				n++;
			}
			mono.Append (')');

			objcSignature = objc.ToString ();
			monoSignature = mono.ToString ();
		}

		public IEnumerable<ConstructorInfo> GetUnavailableParentCtors (Type type, List<ConstructorInfo> typeCtors)
		{
			var baseType = type.BaseType;
			if (baseType.Namespace == "System" && baseType.Name == "Object")
				return Enumerable.Empty<ConstructorInfo> ();

			List<ConstructorInfo> parentCtors;
			if (!ctors.TryGetValue (baseType, out parentCtors))
				return Enumerable.Empty<ConstructorInfo> ();

			var finalList = new List<ConstructorInfo> ();
			foreach (var pctor in parentCtors) {
				var pctorParams = pctor.GetParameters ();
				foreach (var ctor in typeCtors) {
					var ctorParams = ctor.GetParameters ();
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