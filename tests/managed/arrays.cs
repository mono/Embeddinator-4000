using System;
using System.Linq;
using Interfaces;

namespace Arrays {
	public class Arr {
		public string [] StringArrMethod () => new [] { "Hola", "Hello", "Bonjour" };
		public ValueHolder [] ValueHolderArrMethod () => new [] { new ValueHolder (1), new ValueHolder (2), new ValueHolder (3) };
		public bool [] BoolArrMethod () => new [] { true, false, true };
		public char [] CharArrMethod () => new [] { 'a', 'b', '@' };
		public double [] DoubleArrMethod () => new [] { 1.5, 5.1, 3.1416 };
		public float [] FloatArrMethod () => new [] { 1.5f, 5.1f, 3.1416f };
		public sbyte [] SbyteArrMethod () => new sbyte [] { 127, -128, 0 };
		public short [] ShortArrMethod () => new short [] { short.MaxValue, short.MinValue, 0 };
		public int [] IntArrMethod () => new [] { int.MaxValue, int.MinValue, 0 };
		public long [] LongArrMethod () => new [] { long.MaxValue, long.MinValue, 0 };
		public ushort [] UshortArrMethod () => new ushort [] { ushort.MaxValue, ushort.MinValue, 10 };
		public uint [] UintArrMethod () => new uint [] { uint.MaxValue, uint.MinValue, 15 };
		public ulong [] UlongArrMethod () => new ulong [] { ulong.MaxValue, ulong.MinValue, 117 };
		public byte [] ByteArrMethod () => new byte [] { 0x0, 0x01, 0x02, 0x04, 0x08 };
		public IMakeItUp [] InterfaceArrMethod () => new [] { Supplier.Create (), Supplier.Create (), Supplier.Create () };

		public string [] GetNull { get; } = null;
		public string [] StringArr { get; } = new [] { "Hola", "Hello", "Bonjour" };
		public int [] IntArr { get; } = new [] { int.MaxValue, int.MinValue, 0 };
		public ValueHolder [] ValueHolderArr { get; } = new [] { new ValueHolder (1), new ValueHolder (2), new ValueHolder (3) };
		public byte [] ByteArr { get; } = new byte [] { 0x0, 0x01, 0x02, 0x04, 0x08 };
		public IMakeItUp [] InterfaceArr { get; } = new [] { Supplier.Create (), Supplier.Create (), Supplier.Create () };

		public string [] StringArrMethod (string [] strArr) => strArr;
		public bool [] BoolArrMethod (bool [] boolArr) => boolArr;
		public char [] CharArrMethod (char [] charArr) => charArr;
		public sbyte [] SbyteArrMethod (sbyte [] sbyteArr) => sbyteArr;
		public short [] ShortArrMethod (short [] shortArr) => shortArr;
		public int [] IntArrMethod (int [] intArr) => intArr;
		public long [] LongArrMethod (long [] longArr) => longArr;
		public ushort [] UshortArrMethod (ushort [] ushortArr) => ushortArr;
		public uint [] UintArrMethod (uint [] uintArr) => uintArr;
		public ulong [] UlongArrMethod (ulong [] ulongArr) => ulongArr;
		public float [] FloatArrMethod (float [] floatArr) => floatArr;
		public double [] DoubleArrMethod (double [] doubleArr) => doubleArr;
		public ValueHolder [] ValueHolderArrMethod (ValueHolder [] valhArr) => valhArr;
		public IMakeItUp [] InterfaceArrMethod (IMakeItUp [] interArr) => interArr;
		public byte [] ByteArrMethod (byte [] byteArr) => byteArr;

		public string [] GetNullMethod () => null;
		public string [] StringNullArrMethod () => new [] { "Hola", null, "Bonjour" };
		public ValueHolder [] ValueHolderNullArrMethod () => new [] { new ValueHolder (1), null, new ValueHolder (3) };
		public IMakeItUp [] InterfaceNullArrMethod () => new [] { Supplier.Create (), null, Supplier.Create () };

		public void StringArrRef (ref string [] strArr) => strArr = strArr?.Reverse ().ToArray ();
		public void LongArrRef (ref long [] longArr) => longArr = longArr?.Reverse ().ToArray ();
		public void ByteArrRef (ref byte [] byteArr) => byteArr = byteArr?.Reverse ().ToArray ();
		public void ValueHolderArrRef (ref ValueHolder [] valueArr) => valueArr = valueArr?.Reverse ().ToArray ();
		public void IMakeItUpArrRef (ref IMakeItUp [] interArr) => interArr = interArr != null ? new [] { Supplier.Create () } : null;

		public enum Enum
		{
			A = 0,
			B,
			C
		}

		public static Enum EnumArrayLast (Enum[] array)
		{
			return array.Last();
		}
		public static int SumByteArray (byte[] array)
		{
			return array.Sum(n => n); 
		}
		
		public static int[] ReturnsIntArray ()
		{
			return new int[] { 1, 2, 3 }; 
		}

		public static string[] ReturnsStringArray ()
		{
			return new string[] { "1", "2", "3" };
		}
	}

	public class ValueHolder {
		public int IntValue { get; private set; }
		public ValueHolder (int intValue)
		{
			IntValue = intValue;
		}
	}
}
