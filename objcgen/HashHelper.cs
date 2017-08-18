using System;

namespace Embeddinator.ObjC {
	public class HashHelper : MethodHelper {
		public HashHelper (ProcessedMethod method, SourceWriter headers, SourceWriter implementation) :
			base (method, headers, implementation)
		{
			MonoSignature = "GetHashCode()";
			ObjCSignature = "hash";
			ReturnType = "NSUInteger";
		}

		public override void WriteHeaders ()
		{
			headers.WriteLine ();
			headers.WriteLine ("/** This override the default hashing computation (defined in NSObject Protocol)");
			headers.WriteLine (" * https://developer.apple.com/reference/objectivec/1418956-nsobject/1418859-hash?language=objc");
			headers.WriteLine (" */");
			base.WriteHeaders ();
		}

		public override void WriteImplementation ()
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
