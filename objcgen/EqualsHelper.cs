using System;
using System.IO;

using Embeddinator;

namespace ObjC {
	public class EqualsHelper : MethodHelper{
		public EqualsHelper (SourceWriter headers, SourceWriter implementation) : base (headers, implementation)
		{
			MonoSignature = "Equals(object)";
			ObjCSignature = "isEqual:(id _Nullable)other";
			ReturnType = "bool";
		}

		public void WriteImplementation ()
		{
			BeginImplementation ();
			implementation.WriteLine ("if (![other respondsToSelector: @selector (xamarinGetGCHandle)])");
			implementation.Indent++;
			implementation.WriteLine ("return false;");
			implementation.Indent--;
			WriteMethodLookup ();

			implementation.WriteLine ("MonoObject* __exception = nil;");
			var instance = "nil";

			implementation.WriteLine ($"MonoObject* __instance = mono_gchandle_get_target (_object->_handle);");
			instance = "__instance";

			implementation.WriteLine ("void* __args [1];");
			implementation.WriteLine ("int gchandle = [other xamarinGetGCHandle];");
			implementation.WriteLine ("__args[0] = mono_gchandle_get_target (gchandle);");

			implementation.Write ("MonoObject* __result = ");
			implementation.WriteLine ($"mono_runtime_invoke (__method, {instance}, __args, &__exception);");

			implementation.WriteLine ("if (__exception)");
			implementation.Indent++;
			if (IgnoreException) {
				// TODO: Apple often do NSLog (or asserts but they are more brutal) and returning nil is allowed (and common)
				implementation.WriteLine ("return nil;");
			} else {
				implementation.WriteLine ("mono_embeddinator_throw_exception (__exception);");
			}
			implementation.Indent--;

			implementation.WriteLine ("void* __unbox = mono_object_unbox (__result);");
			implementation.WriteLine ("return *((bool*)__unbox);");

			EndImplementation ();
		}
	}
}
