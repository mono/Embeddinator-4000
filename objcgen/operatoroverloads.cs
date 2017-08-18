using System;
using System.Collections.Generic;
using System.Linq;
using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

namespace Embeddinator
{
	public static class OperatorOverloads
	{
		delegate IEnumerable<MethodInfo> CustomOperationAction (OperatorInfo info, IEnumerable<MethodInfo> methods, IEnumerable<MethodInfo> equals);

		class OperatorInfo
		{
			public string MetadataName { get; set; }
			public string FriendlyName { get; set; }
			public string ObjCName { get; set; }

			public int ArgumentCount { get; set; }
			public CustomOperationAction CustomAction { get; set; }

			public OperatorInfo (string metadataName, string friendlyName, string objcName, int argumentCount, CustomOperationAction customAction = null)
			{
				MetadataName = metadataName;
				FriendlyName = friendlyName;
				ObjCName = objcName;
				ArgumentCount = argumentCount;
				CustomAction = customAction;
			}

			public bool Contains (string name) => MetadataName == name || FriendlyName == name;
		}

		public static string GetObjCName (string name, int argCount)
		{
			OperatorInfo match = OperatorMapping.FirstOrDefault (x => x.MetadataName == name || x.FriendlyName == name);
			if (match != null && argCount == match.ArgumentCount)
				return match.ObjCName;
			return null;
		}

		public static bool MatchesOperatorFriendlyName (MethodInfo method)
		{
			OperatorInfo possibleMatch = OperatorMapping.FirstOrDefault (x => x.FriendlyName == method.Name && x.ArgumentCount == method.GetParameters ().Length);
			if (possibleMatch != null) {
				// To be considered a "friendly" operator it must have first argument and return its type
				// TODO - Is there a better huristic
				var param = method.GetParameters ();
				if (method.ReturnType == method.DeclaringType && param[0].ParameterType == method.DeclaringType)
					return true;
			}
			return false;
		}

		// From https://msdn.microsoft.com/en-us/library/ms229032(v=vs.110).aspx and https://msdn.microsoft.com/en-us/library/8edha89s.aspx
		// with ambigious elements and non-overridable elements removed
		static List<OperatorInfo> OperatorMapping = new List<OperatorInfo>() { 
			new OperatorInfo ("op_Addition", "Add", "add", 2),
			new OperatorInfo ("op_Subtraction", "Subtract", "subtract", 2),
			new OperatorInfo ("op_Multiply", "Multiply", "multiply", 2),
			new OperatorInfo ("op_Division", "Divide", "divide", 2),
			new OperatorInfo ("op_ExclusiveOr", "Xor", "xor", 2),
			new OperatorInfo ("op_BitwiseAnd", "BitwiseAnd", "bitwiseAnd", 2),
			new OperatorInfo ("op_BitwiseOr", "BitwiseOr", "bitwiseOr", 2),
			new OperatorInfo ("op_LeftShift", "LeftShift", "leftShift", 2),
			new OperatorInfo ("op_RightShift", "RightShift", "rightShift", 2),
			new OperatorInfo ("op_Decrement", "Decrement", "decrement", 1),
			new OperatorInfo ("op_Increment", "Increment", "increment", 1),
			new OperatorInfo ("op_UnaryNegation", "Negate", "negate", 1),
			new OperatorInfo ("op_UnaryPlus", "Plus", "plus", 1),
			new OperatorInfo ("op_OnesComplement", "OnesComplement", "onesComplement", 1),
			new OperatorInfo ("op_Equality", "Equals", "areEqual", 2, HandleEquals),
			new OperatorInfo ("op_Inequality", "", "", 2, HandleEquals),
		};

		// We will be asking if a method is possibly an operator often in a loop, so cache
		static Dictionary<string, OperatorInfo> OperatorMappingNames;

		static OperatorOverloads ()
		{
			OperatorMappingNames = new Dictionary<string, OperatorInfo> ();
			foreach (var element in OperatorMapping) {
				OperatorMappingNames.Add (element.MetadataName, element);
				OperatorMappingNames.Add (element.FriendlyName, element);
			}
		}

		public static IEnumerable <MethodInfo> FindOperatorPairToIgnore (IEnumerable<ProcessedMethod> methods, IEnumerable <MethodInfo> equals)
		{
			var matches = new Dictionary<OperatorInfo, List<MethodInfo>> ();
			foreach (var method in methods) {
				OperatorInfo info;
				if (OperatorMappingNames.TryGetValue (method.Method.Name, out info) && method.Method.GetParameters ().Length == info.ArgumentCount)
					matches.AddValue (info, method.Method);
			}

			foreach (var match in matches) {
				if (match.Key.CustomAction != null) {
					foreach (MethodInfo ignore in match.Key.CustomAction (match.Key, match.Value, equals))
						yield return ignore;
					continue;
				}

				if (match.Value.Count > 1) {
					// If we have a friendly method, ignore op variants and vice versa
					MethodInfo friendlyMethod = match.Value.FirstOrDefault (x => !x.Name.StartsWith ("op_", StringComparison.Ordinal));
					if (friendlyMethod != null) {
						foreach (var opMethod in match.Value.Where (x => x.Name.StartsWith ("op_", StringComparison.Ordinal)))
							yield return opMethod;
					}
					else {
						yield return friendlyMethod;
					}
				}
			}
		}

		static IEnumerable<MethodInfo> HandleEquals (OperatorInfo info, IEnumerable<MethodInfo> methods, IEnumerable<MethodInfo> equals)
		{
			foreach (var method in equals)
				yield return method;
			foreach (var method in methods.Where (x => x.Name == "op_Inequality"))
				yield return method;
		}
	}
}
