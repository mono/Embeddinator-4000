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

		public static Dictionary<string, string> TypeToArgument = new Dictionary<string, string> {
			{ "int", "anInt" },
			{ "uint", "aUint" },
			{ "double", "aDouble" },
			{ "float", "aFloat" },
			{ "NSString *", "aString" },
			{ "id", "anObject" },
			{ "NSObject", "anObject" },
			{ "NSPoint", "aPoint" },
			{ "NSRect", "aRect" },
			{ "NSFont", "fontObj" },
			{ "SEL", "aSelector" },
			{ "short", "aShort" },
			{ "ushort", "aUshort" },
			{ "long", "aLong" },
			{ "ulong", "aUlong" },
			{ "bool", "aBool" },
			{ "char", "aChar" },
		};

		void GetSignatures (string objName, string monoName, MemberInfo info, ParameterInfo [] parameters, bool useTypeNames, bool isExtension, out string objcSignature, out string monoSignature)
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
				string paramName = useTypeNames ? p.ParameterType.Name : p.Name;
				if ((method != null) && (n > 0 || !isExtension)) {
					if (n == 0) {
						bool isPropertyMethod = method.IsSpecialName && (method.Name.StartsWith ("get") || method.Name.StartsWith ("set"));
						if (method.IsConstructor || !method.IsSpecialName || (useTypeNames && isPropertyMethod))
							objc.Append (paramName.PascalCase ());
					} else
						objc.Append (paramName.ToLowerInvariant ());
				}
				var pt = p.ParameterType;
				var ptname = NameGenerator.GetTypeName (p.ParameterType);
				if (types.Contains (pt))
					ptname += " *";
				string pName = p.Name;
				if (p.Name.Length < 3) {
					if (TypeToArgument.ContainsKey (ptname))
						pName = TypeToArgument [ptname] + p.Name.PascalCase ();
					else pName = "anObject" + p.Name.PascalCase ();
				}
				if (n > 0 || !isExtension)
					objc.Append (":(").Append (ptname).Append (")").Append (pName);
				mono.Append (NameGenerator.GetMonoName (p.ParameterType));
				n++;
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
