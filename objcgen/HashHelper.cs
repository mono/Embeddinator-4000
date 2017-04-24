using System;
using System.IO;

namespace ObjC {
	public class HashHelper : MethodHelper {
		public HashHelper (TextWriter headers, TextWriter implementation) : base (headers, implementation)
		{
			MonoSignature = "GetHashCode ()";
			ReturnType = "NSUInteger";
		}

		public void WriteImplementation ()
		{
			BeginImplementation ();
			WriteMethodLookup ();
			WriteInvoke ("nil");

			implementation.WriteLine ("\tvoid* __unbox = mono_object_unbox (__result);");
			implementation.WriteLine ("\treturn (NSUInteger)(*((int*)__unbox));");
			EndImplementation ();
		}
	}
}
