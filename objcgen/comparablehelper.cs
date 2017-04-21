using System;
using System.IO;

namespace ObjC {
	
	public class ComparableHelper : MethodHelper {

		public ComparableHelper (TextWriter headers, TextWriter implementation) :
			base (headers, implementation)
		{
			ReturnType = "NSComparisonResult";
		}

		public void WriteImplementation ()
		{
			BeginImplementation ();
			WriteMethodLookup ();
			implementation.WriteLine ($"\treturn mono_embeddinator_compare_to (_object, __method, other ? other->_object : nil);");
			EndImplementation ();
		}
	}
}
