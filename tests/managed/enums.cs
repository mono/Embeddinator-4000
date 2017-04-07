#if NON_OBJC_SUPPORTED_TESTS

using System;
using System.Linq;

namespace Enums {

	public enum Enum
	{
		Two = 2,
		Three
	}

	public enum EnumByte : byte
	{
		Two = 2,
		Three
	}

	[Flags]
	public enum EnumFlags
	{
		FlagOne = 1 << 0,
		FlagTwo = 1 << 2
	}

	public static class EnumTypes {

		public static int PassEnum (Enum e) { return (int)e; }
		public static byte PassEnumByte (EnumByte e) { return (byte)e; }
		public static int PassEnumFlags (EnumFlags e) { return (int)e; }
	}
}

#endif