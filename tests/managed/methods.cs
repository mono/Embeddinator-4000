using System;
using System.Collections.Generic;

namespace Methods {
	
	public class Static  {

		// not exposed
		private Static (int id)
		{
			Id = id;
		}

		public static Static Create (int id)
		{
			return new Static (id);
		}

		// to help test the method call was successful
		// only getter will be generated
		public int Id { get; private set; }
	}

	public class Parameters {

		public static string Concat (string first, string second)
		{
			if (first == null)
				return second;
			if (second == null)
				return first;
			return first + second;
		}

		public static void Ref (ref bool boolean, ref string @string)
		{
			boolean = !boolean;
			@string = @string == null ? "hello" : null;
		}

		public static void Out (string @string, out int length, out string upper)
		{
			length = @string == null ? 0 : @string.Length;
			upper =  @string == null ? null : @string.ToUpperInvariant ();
		}
	}

	public class Item {

		// not generated
		internal Item (int id)
		{
			Integer = id;
		}

		public int Integer { get; private set; }
	}

	public static class Factory {

		public static Item CreateItem (int id = 0)
		{
			return new Item (id);
		}

		public static Item ReturnNull () => null;
	}

	public class Collection {

		internal List<Item> c = new List<Item> ();

		public void Add (Item item)
		{
			c.Add (item);
		}

		public void Remove (Item item)
		{
			c.Remove (item);
		}

		public int Count => c.Count;

		public Item this [int index] {
			get { return c [index]; }
			set { c [index] = value; }
		}
	}

	public class DuplicateMethods {
		public int DoIt () { return 42; }
		public int DoIt (int i) { return 42; }
		public int DoIt (string s) { return 42; }
		public int DoIt (int i, int j) { return 84; }

		public bool Find (string name) { return true; }
		public bool Find (string firstName, string lastName) { return true; }
	}
	
	// Three extensions on two different types and a _normal_ static method
	// objc: categories are per type (2 different here) and one should be a _normal_ method
	public static class SomeExtensions {

		public static int CountNonNull (this Collection collection)
		{
			int n = 0;
			foreach (var i in collection.c) {
				if (i != null)
					n++;
			}
			return n;
		}

		public static int CountNull (this Collection collection)
		{
			return collection.Count - collection.CountNonNull ();
		}

		public static bool IsEmptyButNotNull (this string @string)
		{
			if (@string == null)
				return false;
			return @string.Length == 0;
		}

		public static string NotAnExtensionMethod ()
		{
			return String.Empty;
		}
	}

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
			return new AllOperators (c1.Value | c2.Value);
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
			return new AllOperators (+ c1.Value);
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
}
