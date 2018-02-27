using System;

using Embeddinator;

namespace ObjC {

	public class MethodHelper {

		protected SourceWriter headers;
		protected SourceWriter implementation;

		public MethodHelper (ProcessedMethod method, SourceWriter headers, SourceWriter implementation)
		{
			AssemblySafeName = method.DeclaringType.Assembly.SafeName;
			MetadataToken = method.Method.MetadataToken;
			ObjCTypeName = method.DeclaringType.ObjCName;
			ManagedTypeName = method.DeclaringType.TypeName;
			this.headers = headers;
			this.implementation = implementation;
		}

		[Obsolete]
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

		public virtual void WriteHeaders ()
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
			implementation.WriteLine ("MONO_THREAD_ATTACH;");
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
				implementation.WriteLine ($"MonoMethod* __virtual_method = mono_object_get_virtual_method ((MonoObject *){instance}, __method);");
				method = "__virtual_method";
			}
			if (!IsConstructor && (ReturnType != "void"))
				implementation.Write ("MonoObject* __result = ");
			implementation.WriteLine ($"mono_runtime_invoke ({method}, {instance}, {args}, &__exception);");

			implementation.WriteLine ("if (__exception) {");
			implementation.Indent++;
			if (IgnoreException) {
				// TODO: Apple often do NSLog (or asserts but they are more brutal) and returning nil is allowed (and common)
				implementation.WriteLine ("NSLog (@\"%@\", mono_embeddinator_get_nsstring (mono_object_to_string (__exception, nil)));");
				implementation.WriteLine ("return nil;");
			} else {
				implementation.WriteLine ("mono_embeddinator_throw_exception (__exception);");
			}
			implementation.Indent--;
			implementation.WriteLine ("}");
		}

		public void EndImplementation ()
		{
			implementation.WriteLine ("MONO_THREAD_DETACH;");
			implementation.Indent--;
			implementation.WriteLine ("}");
			implementation.WriteLine ();
		}

		public virtual void WriteImplementation ()
		{
			throw new NotImplementedException ();
		}
	}
}
