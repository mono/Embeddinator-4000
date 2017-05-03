using System;
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
		public byte [] ByteArrMethod () => new byte[] { 0x0, 0x01, 0x02, 0x04, 0x08 };
	}

	public class ValueHolder {
		public int IntValue { get; private set; }
		public ValueHolder (int intValue)
		{
			IntValue = intValue;
		}
	}
}
