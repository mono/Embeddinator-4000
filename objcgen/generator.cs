using System;
using System.Collections.Generic;
using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

namespace Embeddinator {

	public abstract class Generator
	{

		public virtual void Generate ()
		{
			if (Processor == null)
				throw ErrorHelper.CreateError (99, "");

			// FIXME: remove asap
			var op = (Processor as ObjC.ObjCProcessor);
			extensions_methods = op.extensions_methods;
			members_with_default_values = op.members_with_default_values;
			subscriptProperties = op.subscriptProperties;
			icomparable = op.icomparable;
			iequatable = op.iequatable;
			equals = op.equals;
			hashes = op.hashes;

			foreach (var a in Processor.Assemblies) {
				Generate (a);
			}
		}

		public Processor Processor { get; set; }

		public bool HasClass (Type t)
		{
			return Processor.Types.HasClass (t);
		}

		public bool HasProtocol (Type t)
		{
			return Processor.Types.HasProtocol (t);
		}

		protected abstract void Generate (ProcessedAssembly a);
		protected abstract void Generate (ProcessedType t);
		protected abstract void Generate (ProcessedProperty property);
		protected abstract void Generate (ProcessedMethod method);
		public abstract void Write (string outputDirectory);

		// to be removed / replaced
		public Dictionary<Type, Dictionary<Type, List<MethodInfo>>> extensions_methods { get; private set; }
		public HashSet<MemberInfo> members_with_default_values { get; private set; }
		public Dictionary<Type, List<ProcessedProperty>> subscriptProperties { get; private set; }
		public Dictionary<Type, MethodInfo> icomparable { get; private set; }
		public Dictionary<Type, MethodInfo> iequatable { get; private set; }
		public Dictionary<Type, MethodInfo> equals { get; private set; }
		public Dictionary<Type, MethodInfo> hashes { get; private set; }
	}
}
