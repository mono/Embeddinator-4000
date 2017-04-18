using System;

namespace Fields {

	public class Class {

		// read only
		public const long MaxLong = Int64.MaxValue;

		public static Class Scratch = new Class (true);

		// read/write
		public static int Integer;

		public bool Boolean;

		public Struct Structure;

		public Class (bool enabled)
		{
			Boolean = enabled;
		}
	}

	public struct Struct {

		// read only
		public static readonly Struct Empty;

		// read/write
		public static Struct Scratch = new Struct ();

		public static int Integer;

		public bool Boolean;

		public Class Class;

		public Struct (bool enabled)
		{
			Boolean = enabled;
			Class = new Class (false);
		}
	}
}
