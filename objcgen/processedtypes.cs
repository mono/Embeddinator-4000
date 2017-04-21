using System;
using System.Collections.Generic;
using System.Linq;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

namespace ObjC {

	// While processing user assemblies, we may come across conditions that will affect
	// final code generation that we need to pass to the generation pass

	public abstract class ProcessedBase {
		public bool FallBackToTypeName { get; set; }
	}

	public class ProcessedMethod : ProcessedBase {
		public MethodInfo Method { get; private set; }

		public ProcessedMethod (MethodInfo method)
		{
			Method = method;
		}
	}

	public class ProcessedProperty: ProcessedBase {
		public PropertyInfo Property { get; private set; }

		public ProcessedProperty (PropertyInfo property)
		{
			Property = property;
		}
	}

	public class ProcessedConstructor : ProcessedBase {
		public ConstructorInfo Constructor { get; private set; }

		public ProcessedConstructor (ConstructorInfo constructor)
		{
			Constructor = constructor;
		}
	}

	public class ProcessedFieldInfo : ProcessedBase {
		public FieldInfo Field { get; private set; }

		public ProcessedFieldInfo (FieldInfo field)
		{
			Field = field;
		}
	}
}