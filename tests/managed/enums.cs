using System;

namespace Enums {

	public enum ByteEnum : byte {
		Zero = 0,
		Max = 255,
	}

	public enum IntEnum /* default */ {
		Min = Int32.MinValue,
		Max = Int32.MaxValue,
	}

	[Flags]
	public enum ByteFlags {
		Empty = 0x0,
		Bit0 = 0x1,
		Bit1 = 0x2,
		Bit2 = 0x4,
		Bit3 = 0x8,
		Bit4 = 0x10,
		Bit5 = 0x20,
		Bit6 = 0x40,
		Bit7 = 0x80,
	}

	public enum ShortEnum : short {
		Min = Int16.MinValue,
		Max = Int16.MaxValue,
	}

	public enum LongEnum : long {
		Max = Int64.MaxValue
	}

	public static class Enumer {

		public static ByteFlags Test (ByteEnum b, ref IntEnum i, out ShortEnum s)
		{
			s = b == ByteEnum.Max ? ShortEnum.Max : ShortEnum.Min;
			i = i == IntEnum.Min ? IntEnum.Max : IntEnum.Min;
			return ByteFlags.Bit5 | ByteFlags.Bit1;
		}
	}
}
