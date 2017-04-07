#if NON_OBJC_SUPPORTED_TESTS

using System;
using System.Linq;

namespace Arrays {

	public static class ArrayTypes {

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
}

#endif