using System;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

namespace Embeddinator {

	public static class TypeExtensions {
		
		public static bool Is (this Type self, string @namespace, string name)
		{
			return (self.Namespace == @namespace) && (self.Name == name);
		}

		public static bool Implements (this Type self, string @namespace, string name)
		{
			if (self.Is ("System", "Object"))
				return false;
			if (self.Is (@namespace, name))
				return true;
			foreach (var intf in self.GetInterfaces ()) {
				if (intf.Is (@namespace, name))
					return true;
			}
			var bt = self.BaseType;
			if (bt == null)
				return false;
			return bt.Implements (@namespace, name);
		}

		public static bool Match (this MethodInfo self, string returnType, string name, params string[] parameterTypes)
		{
			if (self.Name != name)
				return false;
			var pc = self.ParameterCount;
			if (pc != parameterTypes.Length)
				return false;
			if (self.ReturnType.FullName != returnType)
				return false;
			var parameters = self.GetParameters ();
			for (int i = 0; i < pc; i++) {
				// parameter type not specified, useful for generics
				if (parameterTypes [i] == null)
					continue;
				if (parameterTypes [i] != parameters [i].ParameterType.FullName)
					return false;
			}
			return true;
		}

		public static bool HasCustomAttribute (this MemberInfo self, string @namespace, string name)
		{
			foreach (var ca in CustomAttributeData.GetCustomAttributes (self)) {
				if (ca.AttributeType.Is (@namespace, name))
					return true;
			}
			return false;
		}
	}
}
