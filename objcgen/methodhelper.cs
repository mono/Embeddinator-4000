using System;
using System.IO;

using Embeddinator;

namespace ObjC {

	public class MethodHelper {

		protected SourceWriter headers;
		protected SourceWriter implementation;

		public MethodHelper (SourceWriter headers, SourceWriter implementation)
		{
			this.headers = headers;
			this.implementation = implementation;
		}

		public string AssemblySafeName { get; set; }

		public bool IsConstructor { get; set; }
		public bool IsExtension { get; set; }
		public bool IsStatic { get; set; }
		public bool IsValueType { get; set; }
		public bool IsVirtual { get; set; }

		public string ReturnType { get; set; }

		public string ObjCSignature { get; set; }
		public string ObjCTypeName { get; set; }

		public string ManagedTypeName { get; set; }

		public int MetadataToken { get; set; }

		public string MonoSignature { get; set; }

		public bool IgnoreException { get; set; }

		void WriteSignature (SourceWriter writer)
		{
			writer.Write (IsStatic && !IsExtension ? '+' : '-');
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
			implementation.Indent++;
		}

		public void WriteMethodLookup ()
		{
			implementation.WriteLine ("static MonoMethod* __method = nil;");
			implementation.WriteLine ("if (!__method) {");
			implementation.WriteLineUnindented ("#if TOKENLOOKUP");
			implementation.Indent++;
			implementation.WriteLine ($"__method = mono_get_method (__{AssemblySafeName}_image, 0x{MetadataToken:X8}, {ObjCTypeName}_class);");
			implementation.WriteLineUnindented ("#else");
			implementation.WriteLine ($"const char __method_name [] = \"{ManagedTypeName}:{MonoSignature}\";");
			implementation.WriteLine ($"__method = mono_embeddinator_lookup_method (__method_name, {ObjCTypeName}_class);");
			implementation.Indent--;
			implementation.WriteLineUnindented ("#endif");
			implementation.WriteLine ("}");
		}

		public void WriteInvoke (string args)
		{
			implementation.WriteLine ("MonoObject* __exception = nil;");
			var instance = "nil";
			if (!IsStatic) {
				if (!IsConstructor)
					implementation.WriteLine ($"MonoObject* __instance = mono_gchandle_get_target (_object->_handle);");
				if (IsValueType) {
					implementation.WriteLine ($"void* __unboxed = mono_object_unbox (__instance);");
					instance = "__unboxed";
				} else {
					instance = "__instance";
				}
			}

			var method = "__method";
			if (IsVirtual) {
				implementation.WriteLine ($"MonoMethod* __virtual_method = mono_object_get_virtual_method ({instance}, __method);");
				method = "__virtual_method";
			}
			if (!IsConstructor && (ReturnType != "void"))
				implementation.Write ("MonoObject* __result = ");
			implementation.WriteLine ($"mono_runtime_invoke ({method}, {instance}, {args}, &__exception);");

			implementation.WriteLine ("if (__exception)");
			implementation.Indent++;
			if (IgnoreException) {
				// TODO: Apple often do NSLog (or asserts but they are more brutal) and returning nil is allowed (and common)
				implementation.WriteLine ("return nil;");
			} else {
				implementation.WriteLine ("mono_embeddinator_throw_exception (__exception);");
			}
			implementation.Indent--;
		}

		public void EndImplementation ()
		{
			implementation.Indent--;
			implementation.WriteLine ("}");
			implementation.WriteLine ();
		}
	}
}
