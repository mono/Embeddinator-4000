using System;
using Embeddinator;
using Type = IKVM.Reflection.Type;

namespace ObjC {
	public class EqualsHelper : MethodHelper{
		public Type ParameterType { get; set; }

		public EqualsHelper (ProcessedMethod method, SourceWriter headers, SourceWriter implementation) :
			base (method, headers, implementation)
		{
			ObjCSignature = "isEqual:(id _Nullable)other";
			MonoSignature = "Equals(object)";
			ReturnType = "bool";
		}

		protected virtual void WriteHeadersComment ()
		{
			headers.WriteLine ("/** This override the default equality check (defined in NSObject Protocol)");
			headers.WriteLine (" * https://developer.apple.com/reference/objectivec/1418956-nsobject/1418795-isequal?language=objc");
			headers.WriteLine (" */");
		}

		public override void WriteHeaders ()
		{
			headers.WriteLine ();
			WriteHeadersComment ();
			base.WriteHeaders ();
		}

		public override void WriteImplementation ()
		{
			BeginImplementation ();
			if (ParameterType == null || !ParameterType.IsPrimitive) {
				implementation.WriteLine ("MonoObject* __other = mono_embeddinator_get_object (other, false);");
				implementation.WriteLine ("if (!__other)");
				implementation.Indent++;
				implementation.WriteLine ("return false;");
				implementation.Indent--;
				WriteMethodLookup ();

				implementation.WriteLine ("void* __args [1];");
				implementation.WriteLine ("__args [0] = __other;");
			} else {
				WriteMethodLookup ();

				implementation.WriteLine ("void* __args [1];");
				implementation.WriteLine ("__args [0] = &other;");
			}

			WriteInvoke ("__args");

			implementation.WriteLine ("void* __unbox = mono_object_unbox (__result);");
			implementation.WriteLine ("return *((bool*)__unbox);");

			EndImplementation ();
		}
	}

	public class EquatableHelper : EqualsHelper {

		public EquatableHelper (ProcessedMethod method, SourceWriter headers, SourceWriter implementation) :
			base (method, headers, implementation)
		{
			ReturnType = "bool";
			var pt = method.Method.GetParameters () [0].ParameterType;
			var objc = NameGenerator.GetTypeName (pt);
			var nullable = !pt.IsPrimitive ? " * _Nullable" : "";
			ParameterType = pt;
			ObjCSignature = $"isEqualTo{objc.PascalCase ()}:({objc}{nullable})other";
			MonoSignature = $"Equals({NameGenerator.GetMonoName (pt)})";
		}

		// we do not want EqualsHelper comment on IEquatable<T> support, it's not applicable
		protected override void WriteHeadersComment ()
		{
		}
	}
}
