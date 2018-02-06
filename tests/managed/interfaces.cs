﻿﻿using System;

namespace Interfaces {

	public interface IMakeItUp {

		bool Boolean { get; }

		string Convert (int integer);

		string Convert (long longint);
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
			return new System.Globalization.CultureInfo (name);
		}

		public static string Format (double value, IFormatProvider provider)
		{
			return String.Format (provider, "{0}", value);
		}
	}

	public interface GenericInterface<T>
	{

	}

	public class ClassWithGenericInterface : GenericInterface<int>, IOperations
	{
		public int AddInt (int a, int b)
		{
			return a + b;
		}	
	}

    public interface IBase
    {
        void Hello();
    }

    public interface IMore : IBase
    {
        void World();
    }

    public class MoreExplicit : IMore
    {
        void IBase.Hello() { }

        void IMore.World() { }
    }

    public interface IConflict
    {
        string TestProperty { get; }

        string TestField { get; }

        void Hello();
    }

    public class Conflicted : IBase, IConflict
    {
        void IBase.Hello() { }

        void IConflict.Hello() { }

        public void Hello() { }

        public void Hello(string foo) { }

        string IConflict.TestProperty
        {
            get { return "IConflict.TestProperty"; }
        }

        public static string TestProperty
        {
            get { return "TestProperty"; }
        }

        string IConflict.TestField
        {
            get { return "IConflict.TestField"; }
        }

        public static string TestField = "TestField";
    }
}
