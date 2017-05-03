using System;
using System.Collections.Generic;

namespace Overloads {
	public class OperatorCollision {
		public int Value { get; private set; }

		public OperatorCollision (int val)
		{
			Value = val;
		}

		// This collides with Add
		public static OperatorCollision operator + (OperatorCollision c1, OperatorCollision c2)
		{
			return new OperatorCollision (c1.Value + c2.Value);
		}

		public static OperatorCollision Add (OperatorCollision c1, OperatorCollision c2)
		{
			return new OperatorCollision (c1.Value + c2.Value);
		}

		// This has no collision
		public static OperatorCollision operator - (OperatorCollision c1, OperatorCollision c2)
		{
			return new OperatorCollision (c1.Value - c2.Value);
		}

		// This has no collision
		public static OperatorCollision Multiply (OperatorCollision c1, OperatorCollision c2)
		{
			return new OperatorCollision (c1.Value * c2.Value);
		}
	}

	public class AllOperators {
		public int Value { get; private set; }

		public AllOperators (int val)
		{
			Value = val;
		}

		public static AllOperators operator + (AllOperators c1, AllOperators c2)
		{
			return new AllOperators (c1.Value + c2.Value);
		}

		public static AllOperators operator - (AllOperators c1, AllOperators c2)
		{
			return new AllOperators (c1.Value - c2.Value);
		}

		public static AllOperators operator * (AllOperators c1, AllOperators c2)
		{
			return new AllOperators (c1.Value * c2.Value);
		}

		public static AllOperators operator / (AllOperators c1, AllOperators c2)
		{
			return new AllOperators (c1.Value / c2.Value);
		}

		// Make sure we can accept multiple operators with different types
		public static AllOperators operator / (AllOperators c1, int c2)
		{
			return new AllOperators (c1.Value / c2);
		}

		public static AllOperators operator / (int c1, AllOperators c2)
		{
			return new AllOperators (c1 / c2.Value);
		}

		public static AllOperators operator / (AllOperators c1, long c2) 
		{
			return new AllOperators (c1.Value / (int)c2);
		}

		public static AllOperators operator ^ (AllOperators c1, AllOperators c2)
		{
			return new AllOperators (c1.Value ^ c2.Value);
		}

		public static AllOperators operator | (AllOperators c1, AllOperators c2)
		{
			return new AllOperators (c1.Value | c2.Value);
		}

		public static AllOperators operator & (AllOperators c1, AllOperators c2)
		{
			return new AllOperators (c1.Value & c2.Value);
		}

		public static AllOperators operator & (AllOperators c1, int c2)
		{
			return new AllOperators (c1.Value & c2);
		}

		public static AllOperators operator ++ (AllOperators c1)
		{
			return new AllOperators (c1.Value + 1);
		}

		public static AllOperators operator -- (AllOperators c1)
		{
			return new AllOperators (c1.Value - 1);
		}

		public static AllOperators operator >> (AllOperators c1, int a)
		{
			return new AllOperators (c1.Value >> a);
		}

		public static AllOperators operator << (AllOperators c1, int a)
		{
			return new AllOperators (c1.Value << a);
		}

		public static AllOperators operator ~ (AllOperators c1)
		{
			return new AllOperators (~c1.Value);
		}

		public static AllOperators operator + (AllOperators c1)
		{
			return new AllOperators (+c1.Value);
		}

		public static AllOperators operator - (AllOperators c1)
		{
			return new AllOperators (-c1.Value);
		}
	}

	public class AllOperatorsWithFriendly {
		public int Value { get; private set; }

		public AllOperatorsWithFriendly (int val)
		{
			Value = val;
		}

		public static AllOperatorsWithFriendly operator + (AllOperatorsWithFriendly c1, AllOperatorsWithFriendly c2)
		{
			return new AllOperatorsWithFriendly (c1.Value + c2.Value);
		}

		public static AllOperatorsWithFriendly Add (AllOperatorsWithFriendly c1, AllOperatorsWithFriendly c2)
		{
			return new AllOperatorsWithFriendly (c1.Value + c2.Value);
		}

		public static AllOperatorsWithFriendly operator - (AllOperatorsWithFriendly c1, AllOperatorsWithFriendly c2)
		{
			return new AllOperatorsWithFriendly (c1.Value - c2.Value);
		}

		public static AllOperatorsWithFriendly Subtract (AllOperatorsWithFriendly c1, AllOperatorsWithFriendly c2)
		{
			return new AllOperatorsWithFriendly (c1.Value - c2.Value);
		}

		public static AllOperatorsWithFriendly operator * (AllOperatorsWithFriendly c1, AllOperatorsWithFriendly c2)
		{
			return new AllOperatorsWithFriendly (c1.Value * c2.Value);
		}

		public static AllOperatorsWithFriendly Multiply (AllOperatorsWithFriendly c1, AllOperatorsWithFriendly c2)
		{
			return new AllOperatorsWithFriendly (c1.Value * c2.Value);
		}

		public static AllOperatorsWithFriendly operator / (AllOperatorsWithFriendly c1, AllOperatorsWithFriendly c2)
		{
			return new AllOperatorsWithFriendly (c1.Value / c2.Value);
		}

		public static AllOperatorsWithFriendly Divide (AllOperatorsWithFriendly c1, AllOperatorsWithFriendly c2)
		{
			return new AllOperatorsWithFriendly (c1.Value / c2.Value);
		}

		public static AllOperatorsWithFriendly operator ^ (AllOperatorsWithFriendly c1, AllOperatorsWithFriendly c2)
		{
			return new AllOperatorsWithFriendly (c1.Value ^ c2.Value);
		}

		public static AllOperatorsWithFriendly Xor (AllOperatorsWithFriendly c1, AllOperatorsWithFriendly c2)
		{
			return new AllOperatorsWithFriendly (c1.Value ^ c2.Value);
		}

		public static AllOperatorsWithFriendly operator | (AllOperatorsWithFriendly c1, AllOperatorsWithFriendly c2)
		{
			return new AllOperatorsWithFriendly (c1.Value | c2.Value);
		}

		public static AllOperatorsWithFriendly BitwiseOr (AllOperatorsWithFriendly c1, AllOperatorsWithFriendly c2)
		{
			return new AllOperatorsWithFriendly (c1.Value | c2.Value);
		}

		public static AllOperatorsWithFriendly operator & (AllOperatorsWithFriendly c1, AllOperatorsWithFriendly c2)
		{
			return new AllOperatorsWithFriendly (c1.Value & c2.Value);
		}

		public static AllOperatorsWithFriendly BitwiseAnd (AllOperatorsWithFriendly c1, AllOperatorsWithFriendly c2)
		{
			return new AllOperatorsWithFriendly (c1.Value & c2.Value);
		}

		public static AllOperatorsWithFriendly operator ++ (AllOperatorsWithFriendly c1)
		{
			return new AllOperatorsWithFriendly (c1.Value + 1);
		}

		public static AllOperatorsWithFriendly Increment (AllOperatorsWithFriendly c1)
		{
			return new AllOperatorsWithFriendly (c1.Value + 1);
		}

		public static AllOperatorsWithFriendly operator -- (AllOperatorsWithFriendly c1)
		{
			return new AllOperatorsWithFriendly (c1.Value - 1);
		}

		public static AllOperatorsWithFriendly Decrement (AllOperatorsWithFriendly c1)
		{
			return new AllOperatorsWithFriendly (c1.Value - 1);
		}

		public static AllOperatorsWithFriendly operator >> (AllOperatorsWithFriendly c1, int a)
		{
			return new AllOperatorsWithFriendly (c1.Value >> a);
		}

		public static AllOperatorsWithFriendly RightShift (AllOperatorsWithFriendly c1, int a)
		{
			return new AllOperatorsWithFriendly (c1.Value >> a);
		}

		public static AllOperatorsWithFriendly operator << (AllOperatorsWithFriendly c1, int a)
		{
			return new AllOperatorsWithFriendly (c1.Value << a);
		}

		public static AllOperatorsWithFriendly LeftShift (AllOperatorsWithFriendly c1, int a)
		{
			return new AllOperatorsWithFriendly (c1.Value << a);
		}

		public static AllOperatorsWithFriendly operator ~ (AllOperatorsWithFriendly c1)
		{
			return new AllOperatorsWithFriendly (~c1.Value);
		}

		public static AllOperatorsWithFriendly OnesComplement (AllOperatorsWithFriendly c1)
		{
			return new AllOperatorsWithFriendly (~c1.Value);
		}

		public static AllOperatorsWithFriendly operator + (AllOperatorsWithFriendly c1)
		{
			return new AllOperatorsWithFriendly (+c1.Value);
		}

		public static AllOperatorsWithFriendly Plus (AllOperatorsWithFriendly c1)
		{
			return new AllOperatorsWithFriendly (+c1.Value);
		}

		public static AllOperatorsWithFriendly Negate (AllOperatorsWithFriendly c1)
		{
			return new AllOperatorsWithFriendly (-c1.Value);
		}

		public static AllOperatorsWithFriendly operator - (AllOperatorsWithFriendly c1)
		{
			return new AllOperatorsWithFriendly (-c1.Value);
		}	
	}

	public class EqualOverrides {
		public int Value { get; private set; }

		public EqualOverrides (int val)
		{
			Value = val;
		}

		public static bool operator == (EqualOverrides a, EqualOverrides b)
		{
			return a.Value == b.Value;
		}

		public static bool operator != (EqualOverrides a, EqualOverrides b)
		{
			return a.Value == b.Value;
		}

		public override bool Equals (Object obj)
		{
			if (obj == null || GetType () != obj.GetType ())
				return false;

			EqualOverrides p = (EqualOverrides)obj;
			return Value == p.Value;
		}

		public override int GetHashCode ()
		{
			return Value.GetHashCode ();
		}
	}
}