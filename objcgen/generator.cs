using System;
using System.Collections.Generic;
using System.Reflection;

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

		protected virtual void Generate (PropertyInfo pi)
		{
		}

		public virtual void Write ()
		{
		}
	}
}
