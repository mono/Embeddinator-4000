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

	public class Generic : IComparable<Generic> {

		public Generic (int i)
		{
			Integer = i;
		}

		public int Integer { get; private set; }

		public int CompareTo (Generic other)
		{
			var i = other == null ? 0 : other.Integer;
			return Integer.CompareTo (i);
		}
	}

	public class Both : IComparable, IComparable<Both> {

		public Both (int i)
		{
			Integer = i;
		}

		public int Integer { get; private set; }

		// this will not be called - we'll prefer the generic version when both are available
		public int CompareTo (object obj)
		{
			throw new NotImplementedException ();
		}

		public int CompareTo (Both other)
		{
			var i = other == null ? 0 : other.Integer;
			return Integer.CompareTo (i);
		}
	}

	// not the common `compare` with same type case - generated "normally"
	public class Different : IComparable<Generic>, IComparable<int> {

		public Different (int i)
		{
			Integer = i;
		}

		public int Integer { get; private set; }

		public int CompareTo (Generic generic)
		{
			var i = generic == null ? 0 : generic.Integer;
			return Integer.CompareTo (i);
		}

		public int CompareTo (int integer)
		{
			return Integer.CompareTo (integer);
		}
	}
}