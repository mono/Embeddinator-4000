using System;

namespace Constructors {

	public class Unique {

		// not generated
		static Unique ()
		{
		}

		// default .ctor is explicit
		// objc: `init`
		public Unique () : this (1)
		{
		}

		// objc: using the parmeter name gives better looking API, e.g. `initWithId:`
		public Unique (int id)
		{
			Id = id;
		}

		// to help test the ctor calls were successful
		// only getter will be generated
		public int Id { get; private set; }
	}

	public class SuperUnique : Unique {

		public SuperUnique () : base (411)
		{
		}

		// note: SuperUnique (int id) {} is NOT exposed on purpose - which means it's not available in C#
		// objc: `init*` are inherited, so it's legal to call `new SuperUnique (42)` because `Unique` as such a .ctor

		// note: `public int Id { get; } should be usable on this type (by inheritance)
	}

	public class Implicit {

		// default ctor is implicit (in C#) and must be generated
		// objc: an `init` method will be generated

		public string TestResult {
			get { return "OK"; }
		}
	}

	public class AllTypeCode {

		// objc: there's no `init` generated here

		public AllTypeCode (bool b1, char c2, string s)
		{
			TestResult = (b1 && (c2 == Char.MaxValue) && (s == "Mono"));
		}

		// signed
		public AllTypeCode (sbyte i8, short i16, int i32, long i64)
		{
			TestResult = ((i8 == SByte.MaxValue) && (i16 == Int16.MaxValue) && (i32 == Int32.MaxValue) && (i64 == Int64.MaxValue));
		}

		// unsigned
		public AllTypeCode (byte u8, ushort u16, uint u32, ulong u64)
		{
			TestResult = ((u8 == Byte.MaxValue) && (u16 == UInt16.MaxValue) && (u32 == UInt32.MaxValue) && (u64 == UInt64.MaxValue));
		}

		// floating points
		public AllTypeCode (float f32, double f64)
		{
			TestResult = ((f32 == Single.MaxValue) && (f64 == Double.MaxValue));
		}

		// helper to ease testing from native code
		public bool TestResult { get; private set; }
	}

	public class DefaultValues {

		public DefaultValues (byte b = 0, short s = 1, int i = 2, long l = 3)
		{
			IsDefault = (b == 0) && (s == 1) && (i == 2) && (l == 3);
		}

		public DefaultValues (int nonDefault, string s = "", float f = Single.NaN, double d = Double.PositiveInfinity, Enums.ByteEnum e = Enums.ByteEnum.Max)
		{
			IsDefault = (s != null) && (s.Length == 0) && Single.IsNaN (f) && Double.IsInfinity (d) && (e == Enums.ByteEnum.Max);
		}

		public bool IsDefault { get; private set; }
	}

	/*
	public class Duplicates {

		// objc: except that the following three .ctors would have an identical name in ObjC, e.g. `initWithByte:Short:Int:Long:`
		// objc: in such case it's better to use types for the signature - but the generated output must be identical each time (not random)
		public Duplicates (byte b, short s, int i, long l)
		{
		}

		// in C# this is a different .ctor from the previous one, and it must be exposed to native code
		// objc: same parameter names as before, but using types makes it a unique signature
		public Duplicates (int b, int s, int i, int l)
		{
		}

		// in C# this is a different .ctor from the previous two, and it must be exposed to native code
		// objc: same parameter names as before, but using types makes it a unique signature
		public Duplicates (byte b, byte s, byte i, byte l)
		{
		}
	}
	*/
}
