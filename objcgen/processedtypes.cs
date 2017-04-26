using System;
using System.Collections.Generic;
using System.Linq;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

namespace Embeddinator {

	// While processing user assemblies, we may come across conditions that will affect
	// final code generation that we need to pass to the generation pass

	public abstract class ProcessedMemberBase {
		public bool FallBackToTypeName { get; set; }
	}

	public class ProcessedAssembly {

		public Assembly Assembly { get; private set; }

		public string Name { get; private set; }
		public string SafeName { get; private set; }

		public ProcessedAssembly (Assembly assembly)
		{
			Assembly = assembly;
			Name = assembly.GetName ().Name;
			SafeName = Name.Sanitize ();
		}
	}

	public class ProcessedMethod : ProcessedMemberBase {
		public MethodInfo Method { get; private set; }
		public bool IsOperator { get; set; }

		public string BaseName => IsOperator ? Method.Name.Substring (3).CamelCase () : Method.Name.CamelCase ();

		public ProcessedMethod (MethodInfo method)
		{
			Method = method;
		}
	}

	public class ProcessedProperty: ProcessedMemberBase {
		public PropertyInfo Property { get; private set; }

		public ProcessedProperty (PropertyInfo property)
		{
			Property = property;
		}
	}

	public class ProcessedConstructor : ProcessedMemberBase {
		public ConstructorInfo Constructor { get; private set; }

		public ProcessedConstructor (ConstructorInfo constructor)
		{
			Constructor = constructor;
		}
	}

	public class ProcessedFieldInfo : ProcessedMemberBase {
		public FieldInfo Field { get; private set; }

		public ProcessedFieldInfo (FieldInfo field)
		{
			Field = field;
		}
	}
}