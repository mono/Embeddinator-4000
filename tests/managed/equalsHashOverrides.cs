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

	public class EquatableClass : IEquatable<Class> {
		public int Y { get; set; }

		public EquatableClass (int y)
		{
			Y = y;
		}
		public override bool Equals (object obj)
		{
			var testObj = obj as EquatableClass;

			if (testObj == null)
				return false;

			return Y == testObj.Y;
		}

		public bool Equals (Class obj)
		{
			return Y == obj.X;
		}

		public override int GetHashCode ()
		{
			return Y.GetHashCode ();
		}
	}

	public class EquatableInt : IEquatable<int> {
		public int Y { get; set; }

		public EquatableInt (int y)
		{
			Y = y;
		}
		public override bool Equals (object obj)
		{
			var testObj = obj as EquatableClass;

			if (testObj == null)
				return false;

			return Y == testObj.Y;
		}

		public bool Equals (int obj)
		{
			return Y == obj;
		}

		public override int GetHashCode ()
		{
			return Y.GetHashCode ();
		}
	}
}
