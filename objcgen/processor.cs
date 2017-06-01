﻿using System;
using System.Collections.Generic;
using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

namespace Embeddinator {

	public abstract partial class Processor {

		public List<ProcessedAssembly> Assemblies { get; set; } = new List<ProcessedAssembly> ();
		public List<ProcessedType> Types { get; set; } = new List<ProcessedType> ();

		Queue<ProcessedAssembly> assemblyQueue = new Queue<ProcessedAssembly> ();
		Queue<ProcessedType> typeQueue = new Queue<ProcessedType> ();
		bool processing_ended;

		protected List<Exception> Delayed = new List<Exception> ();

		public virtual void Process (IEnumerable<ProcessedAssembly> input)
		{
			foreach (var a in input) {
				// ignoring/warning one is not an option as they could be different (e.g. different builds/versions)
				if (!AddIfUnique (a))
					throw ErrorHelper.CreateError (12, $"The assembly name `{a.Name}` is not unique");
				assemblyQueue.Enqueue (a);
			}

			while (assemblyQueue.Count > 0) {
				Process (assemblyQueue.Dequeue ());
			}
			processing_ended = true;
		}

		protected abstract IEnumerable<Type> GetTypes (Assembly a);

		public void Process (ProcessedAssembly a)
		{
			if (!a.UserCode)
				return;

			foreach (var t in GetTypes (a.Assembly)) {
				var pt = new ProcessedType (t) {
					Assembly = a,
				};
				Types.Add (pt);

				foreach (var nt in t.GetNestedTypes ()) {
					var pnt = new ProcessedType (nt) {
						Assembly = a
					};
					Types.Add (pnt);
				}
			}

			// we can add new types while processing some (e.g. categories)
			foreach (var type in Types)
				typeQueue.Enqueue (type);
			Types.Clear (); // reuse
			while (typeQueue.Count > 0) {
				Process (typeQueue.Dequeue ());
			}
		}

		public abstract void Process (ProcessedType pt);

		protected void AddExtraType (ProcessedType pt)
		{
			typeQueue.Enqueue (pt);
			// extra types are (most likely) outside the input list of assemblies
			AddIfUnique (pt.Assembly);
		}

		// useful to get BaseType - but can only be called (safely) once processing is done
		protected ProcessedType GetProcessedType (Type t)
		{
			if (!processing_ended)
				throw ErrorHelper.CreateError (99, "Internal error `Invalid state for GetProcessedType`. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).");
			return Types.Find ((pt) => pt.Type == t);
		}

		// uniqueness checks
		HashSet<string> assembly_name = new HashSet<string> ();
		HashSet<string> assembly_safename = new HashSet<string> ();

		bool AddIfUnique (ProcessedAssembly assembly)
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
