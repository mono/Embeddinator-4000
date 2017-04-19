using System;

namespace Comparable {

	public class Class : IComparable {

		public Class (int i)
		{
			Integer = i;
		}

		public int Integer { get; private set; }

		public int CompareTo (object obj)
		{
			var c = obj as Class;
			var i = (c == null) ? 0 : c.Integer;
			return Integer.CompareTo (i);
		}
	}
}