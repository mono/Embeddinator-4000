using System;
using System.IO;

namespace ObjC {

	public class MethodHelper {

		protected TextWriter headers;
		protected TextWriter implementation;

		public MethodHelper (TextWriter headers, TextWriter implementation)
		{
			this.headers = headers;
			this.implementation = implementation;
		}

		public string AssemblyName { get; set; }

		public bool IsConstructor { get; set; }
		public bool IsStatic { get; set; }
		public bool IsValueType { get; set; }

		public string ReturnType { get; set; }

		public string ObjCSignature { get; set; }
		public string ObjCTypeName { get; set; }

		public string ManagedTypeName { get; set; }

		public int MetadataToken { get; set; }

		public string MonoSignature { get; set; }

		public bool IgnoreException { get; set; }

		void WriteSignature (TextWriter writer)
		{
			writer.Write (IsStatic ? '+' : '-');
			writer.Write ($" ({ReturnType}){ObjCSignature}");
		}

		public void WriteHeaders ()
		{
			WriteSignature (headers);
			headers.WriteLine (';');
		}

		public void BeginImplementation ()
		{
			WriteSignature (implementation);
			implementation.WriteLine ();
			implementation.WriteLine ("{");
		}

		public void WriteMethodLookup ()
		{
			implementation.WriteLine ("\tstatic MonoMethod* __method = nil;");
			implementation.WriteLine ("\tif (!__method) {");
			implementation.WriteLine ("#if TOKENLOOKUP");
			implementation.WriteLine ($"\t\t__method = mono_get_method (__{AssemblyName}_image, 0x{MetadataToken:X8}, {ObjCTypeName}_class);");
			implementation.WriteLine ("#else");
			implementation.WriteLine ($"\t\tconst char __method_name [] = \"{ManagedTypeName}:{MonoSignature}\";");
			implementation.WriteLine ($"\t\t__method = mono_embeddinator_lookup_method (__method_name, {ObjCTypeName}_class);");
			implementation.WriteLine ("#endif");
			implementation.WriteLine ("\t}");
		}

		public void WriteInvoke (string args)
		{
			implementation.WriteLine ("\tMonoObject* __exception = nil;");
			var instance = "nil";
			if (!IsStatic) {
				if (!IsConstructor)
					implementation.WriteLine ($"\tMonoObject* __instance = mono_gchandle_get_target (_object->_handle);");
				if (IsValueType) {
					implementation.WriteLine ($"\t\tvoid* __unboxed = mono_object_unbox (__instance);");
					instance = "__unboxed";
				} else {
					instance = "__instance";
				}
			}

			implementation.Write ("\t");
			if (!IsConstructor && (ReturnType != "void"))
				implementation.Write ("MonoObject* __result = ");
			implementation.WriteLine ($"mono_runtime_invoke (__method, {instance}, {args}, &__exception);");

			implementation.WriteLine ("\tif (__exception)");
			if (IgnoreException) {
				// TODO: Apple often do NSLog (or asserts but they are more brutal) and returning nil is allowed (and common)
				implementation.WriteLine ("\t\t\treturn nil;");
			} else {
				implementation.WriteLine ("\t\tmono_embeddinator_throw_exception (__exception);");
			}
		}

		public void EndImplementation ()
		{
			implementation.WriteLine ("}");
			implementation.WriteLine ();
		}
	}
}
