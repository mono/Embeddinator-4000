using System;
namespace EqualsHashOverrides {
	public class Class {
		public int X { get; set; }

		public Class (int x)
		{
			X = x;
		}
		public override bool Equals (object obj)
		{
			var testObj = obj as Class;

			if (testObj == null)
				return false;

			return X == testObj.X;
		}

		public override int GetHashCode ()
		{
			return X.GetHashCode ();
		}
	}
}
