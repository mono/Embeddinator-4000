using System;
using System.IO;

namespace ObjC {
	public class EqualsHelper : MethodHelper{
		public EqualsHelper (TextWriter headers, TextWriter implementation) : base (headers, implementation)
		{
			MonoSignature = "Equals (object)";
			ReturnType = "bool";
		}

		public void WriteImplementation ()
		{
			BeginImplementation ();
			implementation.WriteLine ("\tif (![other respondsToSelector: @selector (xamarinGetGCHandle)])");
			implementation.WriteLine ("\t\treturn false;");
			WriteMethodLookup ();

			implementation.WriteLine ("\tMonoObject* __exception = nil;");
			var instance = "nil";

			implementation.WriteLine ($"\tMonoObject* __instance = mono_gchandle_get_target (_object->_handle);");
			instance = "__instance";

			implementation.WriteLine ("\tvoid* __args [1];");
			implementation.WriteLine ("\tint gchandle = [other xamarinGetGCHandle];");
			implementation.WriteLine ("\t__args[0] = mono_gchandle_get_target (gchandle);");

			implementation.Write ("\tMonoObject* __result = ");
			implementation.WriteLine ($"\tmono_runtime_invoke (__method, {instance}, __args, &__exception);");

			implementation.WriteLine ("\tif (__exception)");
			if (IgnoreException) {
				// TODO: Apple often do NSLog (or asserts but they are more brutal) and returning nil is allowed (and common)
				implementation.WriteLine ("\t\t\treturn nil;");
			} else {
				implementation.WriteLine ("\t\tmono_embeddinator_throw_exception (__exception);");
			}

			implementation.WriteLine ("\tvoid* __unbox = mono_object_unbox (__result);");
			implementation.WriteLine ("\treturn *((bool*)__unbox);");

			EndImplementation ();
		}
	}
}
