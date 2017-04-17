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

		public static Item CreateItem (int id)
		{
			return new Item (id);
		}
	}

	public class Collection {

		List<Item> c = new List<Item> ();

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
}
