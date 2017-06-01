using System;
using System.Collections.Generic;
using System.Text;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

namespace Embeddinator {

	public static class StringExtensions {

		public static string CamelCase (this string self)
		{
			if (self == null)
				return null;
			if (self.Length == 0)
				return String.Empty;
			return Char.ToLowerInvariant (self [0]) + self.Substring (1, self.Length - 1);
		}

		public static string CamelCase (this string self, bool containsWhitespaces)
		{
			if (self == null)
				return null;
			if (self.Length == 0)
				return String.Empty;

			var strings = self.Trim ().Split (' ');
			var count = strings.Length;
			string res = string.Empty;

			for (int i = 0; i < count; i++) {
				if (i == 0)
					res += strings [i].CamelCase ();
				else
					res += strings [i].PascalCase ();
			}

			return res;
		}

		public static string PascalCase (this string self)
		{
			if (self == null)
				return null;
			if (self.Length == 0)
				return String.Empty;
			return Char.ToUpperInvariant (self [0]) + self.Substring (1, self.Length - 1);
		}

		public static string PascalCase (this string self, bool containsWhitespaces)
		{
			if (self == null)
				return null;
			if (self.Length == 0)
				return String.Empty;

			var strings = self.Trim ().Split (' ');
			var count = strings.Length;
			string res = string.Empty;

			for (int i = 0; i < count; i++)
				res += strings [i].PascalCase ();

			return res;
		}

		public static string Sanitize (this string self)
		{
			if (self == null)
				return null;

			StringBuilder sb = null;

			for (int i = 0; i < self.Length; i++) {
				var ch = self [i];
				switch (ch) {
				case '.':
				case '+':
				case '/':
				case '`':
				case '@':
				case '<':
				case '>':
				case '$':
				case '-':
				case ' ':
					if (sb == null)
						sb = new StringBuilder (self, 0, i, self.Length);
					sb.Append ('_');
					break;
				default:
					if (sb != null)
						sb.Append (ch);
					break;
				}
			}

			if (sb != null)
				return sb.ToString ();
			return self;
		}

		public static bool StartsWithOrdinal (this string self, string value)
		{
			return self.StartsWith (value, StringComparison.Ordinal);
		}
	}

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

		public static bool IsPropertyMethod (this MethodBase method) => method.IsSpecialName && (method.Name.StartsWithOrdinal ("get") || method.Name.StartsWithOrdinal ("set"));
		public static bool IsOperatorMethod (this MethodBase method) => method.IsSpecialName && method.Name.StartsWithOrdinal ("op_");
	}

	public static class DictionaryExtensions {
		public static void IncrementValue<TKey> (this Dictionary<TKey, int> dictionary, TKey key)
		{
			if (dictionary.ContainsKey (key))
				dictionary[key] += 1;
			else
				dictionary[key] = 1;
		}

		public static void AddValue<TKey, TListValue> (this Dictionary<TKey, List<TListValue>> dictionary, TKey key, TListValue val)
		{
			if (dictionary.ContainsKey (key))
				dictionary[key].Add (val);
			else
				dictionary[key] = new List<TListValue> () { val };
		}
	}

	public static class ListExtensions {
		public static bool HasClass (this List<ProcessedType> list, Type type)
		{
			foreach (ProcessedType t in list) {
				if (t.IsClass && (type == t.Type))
					return true;
			}
			return false;
		}

		public static bool HasProtocol (this List<ProcessedType> list, Type type)
		{
			foreach (ProcessedType t in list) {
				if (t.IsProtocol && (type == t.Type))
					return true;
			}
			return false;
		}
	}

	public static class EnumerableExtensions {
		public static IEnumerable<T> Yield<T> (this T item)
		{
			yield return item;
		}
	}

}
