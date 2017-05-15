using System;
using System.Linq;

public static class Type_SByte
{
	public static sbyte Max { get { return sbyte.MaxValue; } }
	public static sbyte Min { get { return sbyte.MinValue; } }
}


public static class Type_Int16
{
	public static short Max { get { return short.MaxValue; } }
	public static short Min { get { return short.MinValue; } }
}


public static class Type_Int32
{
	public static int Max { get { return int.MaxValue; } }
	public static int Min { get { return int.MinValue; } }
}


public static class Type_Int64
{
	public static long Max { get { return long.MaxValue; } }
	public static long Min { get { return long.MinValue; } }
}


public static class Type_Byte
{
	public static byte Max { get { return byte.MaxValue; } }
	public static byte Min { get { return byte.MinValue; } }
}


public static class Type_UInt16
{
	public static ushort Max { get { return ushort.MaxValue; } }
	public static ushort Min { get { return ushort.MinValue; } }
}


public static class Type_UInt32
{
	public static uint Max { get { return uint.MaxValue; } }
	public static uint Min { get { return uint.MinValue; } }
}

public static class Type_UInt64
{
	public static ulong Max { get { return ulong.MaxValue; } }
	public static ulong Min { get { return ulong.MinValue; } }
}

public static class Type_Single
{
	public static float Max { get { return float.MaxValue; } }
	public static float Min { get { return float.MinValue; } }
}

public static class Type_Double
{
	public static double Max { get { return double.MaxValue; } }
	public static double Min { get { return double.MinValue; } }
}

public static class Type_Char
{
	public static char Max { get { return char.MaxValue; } }
	public static char Min { get { return char.MinValue; } }
	public static char Zero { get { return (char) 0; } }
}

public static class Type_String
{
	public static string NullString { get { return null; } }
	public static string EmptyString { get { return string.Empty; } }
	public static string NonEmptyString { get { return "Hello World"; } }
}

// objc: this type won't be generated (Exception is not supported) but the generation will succeed (with warnings)
public class MyException : Exception {
}

// objc: this type won't be generated (subclassing an unsupported type) but the generation will succeed (with warnings)
public class MyNextException : MyException {
}

public static class Type_Decimal {
	public static decimal Max { get; } = decimal.MaxValue;
	public static decimal Min { get; } = decimal.MinValue;
	public static decimal Zero { get; } = decimal.Zero;
	public static decimal One { get; } = decimal.One;
	public static decimal MinusOne { get; } = decimal.MinusOne;
	public static decimal Pi { get; } = 3.14159265358979323846264m;
	public static decimal MinusTau { get; } = -6.28318530717958647692m;
	public static decimal FortyTwo { get; } = 42m;
	public static decimal [] DecArr { get; } = { Max, Min, Zero, One, MinusOne, Pi, MinusTau, FortyTwo };

	public static decimal GetDecimal (decimal dec) => dec;
	public static void GetRefPi (ref decimal dec) => dec = Pi;
	public static decimal [] GetDecimalArr (decimal [] dec) => dec;
	public static void ReverseDecimalArrRef (ref decimal [] decArr) => decArr = decArr?.Reverse ().ToArray ();
}

public class ExposeExtraTypes {

	public TimeSpan TimeOfDay {
		get { return DateTime.Now.TimeOfDay; }
	}
}

public static class Type_DateTime {
	public static DateTime ReturnDate (DateTime datetime) => datetime;
	public static void RefDate (ref DateTime datetime) => datetime = DateTime.MinValue;
	public static DateTime [] ReverseDates (DateTime [] dates) => dates?.Reverse ().ToArray ();
	public static void ReverseRefDates (ref DateTime [] dates) => dates = dates?.Reverse ().ToArray ();

	public static DateTime Max { get; } = DateTime.MaxValue;
	public static DateTime Min { get; } = DateTime.MinValue;
}
