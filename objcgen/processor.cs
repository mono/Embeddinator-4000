using System;
using System.Collections.Generic;
using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

namespace Embeddinator {

	public abstract partial class Processor {

		public List<ProcessedAssembly> Assemblies { get; set; } = new List<ProcessedAssembly> ();
		public List<ProcessedType> Types { get; set; } = new List<ProcessedType> ();

		Queue<ProcessedAssembly> AssemblyQueue = new Queue<ProcessedAssembly> ();
		protected List<Exception> delayed = new List<Exception> ();

		public virtual void Process (IEnumerable<Assembly> input)
		{
			foreach (var a in input) {
				var pa = new ProcessedAssembly (a) {
					UserCode = true,
				};
				// ignoring/warning one is not an option as they could be different (e.g. different builds/versions)
				if (!AddIfUnique (pa))
					throw ErrorHelper.CreateError (12, $"The assembly name `{pa.Name}` is not unique");
				AssemblyQueue.Enqueue (pa);
			}

			while (AssemblyQueue.Count > 0) {
				Process (AssemblyQueue.Dequeue ());
			}

			// we can add new types while processing some (e.g. categories)
			var typeQueue = new Queue<ProcessedType> (Types);
			Types.Clear (); // reuse
			while (typeQueue.Count > 0) {
				Process (typeQueue.Dequeue ());
			}
		}

		protected abstract IEnumerable<Type> GetTypes (Assembly a);

		public virtual void Process (ProcessedAssembly a)
		{
			if (!a.UserCode)
				return;

			foreach (var t in GetTypes (a.Assembly)) {
				var pt = new ProcessedType (t) {
					Assembly = a,
				};
				Types.Add (pt);
			}
		}

		public abstract void Process (ProcessedType pt);

		// useful to get BaseType - but can only be called (safely) once processing is done
		public ProcessedType GetProcessedType (Type t)
		{
			return Types.Find ((pt) => pt.Type == t);
		}

		// uniqueness checks
		HashSet<string> assembly_name = new HashSet<string> ();
		HashSet<string> assembly_safename = new HashSet<string> ();

		public bool AddIfUnique (ProcessedAssembly assembly)
		{
			if (assembly_name.Contains (assembly.Name))
				return false;
			if (assembly_safename.Contains (assembly.SafeName))
				return false;

			Assemblies.Add (assembly);
			assembly_name.Add (assembly.Name);
			assembly_safename.Add (assembly.SafeName);
			return true;
		}
	}
}
