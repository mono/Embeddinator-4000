using System;
namespace NestedClasses {
	public class ParentClass {
	    public class NestedClass {
			public int X;
			public int Y;
			public int Sum;

			public NestedClass ()
			{
			}

			public int Addition (int x, int y)
			{
				X = x;
				Y = y;
				Sum = X + Y;

				return Sum;
			}
		}

		public NestedClass Nested { get; set; } = new NestedClass ();

		public int AddNumbers (int x, int y)
		{
			return Nested.Addition (x, y);
		}

		public int Sum {
			get {
				return Nested.Sum;
			}
		}
	}
}
