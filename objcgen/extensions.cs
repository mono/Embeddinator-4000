using System;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

namespace Embeddinator {

	public static class TypeExtensions {
		
		public static bool Is (this Type self, string @namespace, string name)
		{
			return (self.Namespace == @namespace) && (self.Name == name);
		}

		public static bool HasCustomAttribute (this Type self, string @namespace, string name)
		{
			foreach (var ca in self.CustomAttributes) {
				if (ca.AttributeType.Is (@namespace, name))
					return true;
			}
			return false;
		}
	}
}
