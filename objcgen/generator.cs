using System;
using System.Collections.Generic;
using IKVM.Reflection;
using ObjC;
using Type = IKVM.Reflection.Type;

namespace Embeddinator {

	public class Generator {

		public virtual void Process (IEnumerable<Assembly> assemblies)
		{
		}

		public virtual void Generate (IEnumerable<Assembly> assemblies)
		{
			foreach (var a in assemblies) {
				Generate (a);
			}
		}

		protected virtual void Generate (Assembly a)
		{
			foreach (var t in a.GetTypes ()) {
				if (!t.IsPublic)
					continue;
				Generate (t);
			}
		}

		protected virtual void Generate (Type t)
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
