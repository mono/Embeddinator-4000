using System;
using System.Collections.Generic;
using IKVM.Reflection;
using ObjC;
using Type = IKVM.Reflection.Type;

namespace Embeddinator {

	public class Generator {

		protected List<ProcessedAssembly> assemblies = new List<ProcessedAssembly> ();

		// uniqueness checks
		HashSet<string> assembly_name = new HashSet<string> ();
		HashSet<string> assembly_safename = new HashSet<string> ();

		public virtual void Process (IEnumerable<Assembly> input)
		{
		}

		public bool AddIfUnique (ProcessedAssembly assembly)
		{
			if (assembly_name.Contains (assembly.Name))
				return false;
			if (assembly_safename.Contains (assembly.SafeName))
				return false;

			assemblies.Add (assembly);
			assembly_name.Add (assembly.Name);
			assembly_safename.Add (assembly.SafeName);
			return true;
		}

		public virtual void Generate ()
		{
		}

		protected virtual void Generate (ProcessedAssembly a)
		{
		}

		protected virtual void Generate (ProcessedType t)
		{
		}

		protected virtual void Generate (ProcessedProperty property)
		{
		}

		protected virtual void Generate (ProcessedMethod method)
		{
		}

		public virtual void Write (string outputDirectory)
		{
		}
	}
}
