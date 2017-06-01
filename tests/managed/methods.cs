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
		public int DoIt (string i) { return 42; }
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

		// objc: this won't be generated until nullable are supported but we want to make sure it does not stop the build with an error
		public static bool? GetNull (this string @string)
		{
			return null;
		}

		// objc: this will be generated as a normal method since categories cannot be created on primitive types
		public static int Increment (this int value)
		{
			return value + 1;
		}

		public static string NotAnExtensionMethod ()
		{
			return String.Empty;
		}
	}
}
