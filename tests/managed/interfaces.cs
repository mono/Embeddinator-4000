﻿using System;

namespace Interfaces {

	public interface IMakeItUp {

		bool Boolean { get; }

		string Convert (int value);

		string Convert (long value);
	}

	// not public - only the contract is exposed thru a static type
	class MakeItUp : IMakeItUp {

		bool result;
		
		public bool Boolean {
			get {
				result = !result;
				return result;
			}
		}

		public string Convert (int integer)
		{
			return integer.ToString ();
		}

		// overload
		public string Convert (long longint)
		{
			return longint.ToString ();
		}
	}

	public static class Supplier {

		static public IMakeItUp Create ()
		{
			return new MakeItUp ();
		}
	}

	public interface IOperations {
		int AddInt (int a, int b);
	}

	public class ManagedAdder: IOperations {

		public int AddInt (int a, int b)
		{
			return a + b;
		}
	}

	public class OpConsumer {
		public static int DoAddition (IOperations ops, int a, int b)
		{
			return ops.AddInt (a, b);
		}

		public static bool TestManagedAdder (int a, int b)
		{
			return DoAddition (new ManagedAdder (), a, b) == (a + b);
		}
	}

	public class ExposeIFormatProvider {

		public static IFormatProvider GetCulture (string name)
		{
			return System.Globalization.CultureInfo.GetCultureInfo (name);
		}

		public static string Format (double value, IFormatProvider provider)
		{
			return String.Format (provider, "{0}", value);
		}
	}
}
