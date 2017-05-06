using System;

using Embeddinator;

namespace ObjC {
	public class EqualsHelper : MethodHelper{
		public EqualsHelper (SourceWriter headers, SourceWriter implementation) : base (headers, implementation)
		{
			MonoSignature = "Equals(object)";
			ObjCSignature = "isEqual:(id _Nullable)other";
			ReturnType = "bool";
		}

		public override void WriteHeaders ()
		{
			headers.WriteLine ();
			headers.WriteLine ("/** This override the default equality check (defined in NSObject Protocol)");
			headers.WriteLine (" * https://developer.apple.com/reference/objectivec/1418956-nsobject/1418795-isequal?language=objc");
			headers.WriteLine (" */");
			base.WriteHeaders ();
		}

		public void WriteImplementation ()
		{
			BeginImplementation ();
			implementation.WriteLine ("MonoObject* __other = mono_embeddinator_get_object (other, false);");
			implementation.WriteLine ("if (!__other)");
			implementation.Indent++;
			implementation.WriteLine ("return false;");
			implementation.Indent--;
			WriteMethodLookup ();

			implementation.WriteLine ("void* __args [1];");
			implementation.WriteLine ("__args [0] = __other;");

			WriteInvoke ("__args");

			implementation.WriteLine ("void* __unbox = mono_object_unbox (__result);");
			implementation.WriteLine ("return *((bool*)__unbox);");

			EndImplementation ();
		}
	}
}
