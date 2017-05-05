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

		struct OperatorInfo
		{
			public string MetadataName { get; set; }
			public string FriendlyName { get; set; }
			public int ArgumentCount { get; set; }
			public CustomOperationAction CustomAction { get; set; }

			public OperatorInfo (string metadataName, string friendlyName, int argumentCount, CustomOperationAction customAction = null)
			{
				MetadataName = metadataName;
				FriendlyName = friendlyName;
				ArgumentCount = argumentCount;
				CustomAction = customAction;
			}

			public bool Contains (string name) => MetadataName == name || FriendlyName == name;
		}

		// From https://msdn.microsoft.com/en-us/library/ms229032(v=vs.110).aspx and https://msdn.microsoft.com/en-us/library/8edha89s.aspx
		// with ambigious elements and non-overridable elements removed
		static List<OperatorInfo> OperatorMapping = new List<OperatorInfo>() { 
			new OperatorInfo ("op_Addition", "Add", 2),
			new OperatorInfo ("op_Subtraction", "Subtract", 2),
			new OperatorInfo ("op_Multiply", "Multiply", 2),
			new OperatorInfo ("op_Division", "Divide", 2),
			new OperatorInfo ("op_ExclusiveOr", "Xor", 2),
			new OperatorInfo ("op_BitwiseAnd", "BitwiseAnd", 2),
			new OperatorInfo ("op_BitwiseOr", "BitwiseOr", 2),
			new OperatorInfo ("op_LeftShift", "LeftShift", 2),
			new OperatorInfo ("op_RightShift", "RightShift", 2),
			new OperatorInfo ("op_Decrement", "Decrement", 1),
			new OperatorInfo ("op_Increment", "Increment", 1),
			new OperatorInfo ("op_UnaryNegation", "Negate", 1),
			new OperatorInfo ("op_UnaryPlus", "Plus", 1),
			new OperatorInfo ("op_OnesComplement", "OnesComplement" , 1),
			new OperatorInfo ("op_Equality", "Equals" , 2, HandleEquals),
			new OperatorInfo ("op_Inequality", "" , 2, HandleEquals),
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

		public static IEnumerable <MethodInfo> FindOperatorPairToIgnore (IEnumerable<MethodInfo> methods, IEnumerable <MethodInfo> equals)
		{
			var matches = new Dictionary<OperatorInfo, List<MethodInfo>> ();
			foreach (var method in methods) {
				OperatorInfo info;
				if (OperatorMappingNames.TryGetValue (method.Name, out info) && method.ParameterCount == info.ArgumentCount)
					matches.AddValue (info, method);			
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
