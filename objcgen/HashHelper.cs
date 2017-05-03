using System;
using System.IO;

using Embeddinator;

namespace ObjC {
	public class HashHelper : MethodHelper {
		public HashHelper (SourceWriter headers, SourceWriter implementation) : base (headers, implementation)
		{
			MonoSignature = "GetHashCode()";
			ObjCSignature = "hash";
			ReturnType = "NSUInteger";
		}

		public void WriteImplementation ()
		{
			BeginImplementation ();
			WriteMethodLookup ();
			WriteInvoke ("nil");

			implementation.WriteLine ("void* __unbox = mono_object_unbox (__result);");
			implementation.WriteLine ("return (NSUInteger)(*((int*)__unbox));");
			EndImplementation ();
		}
	}
}
