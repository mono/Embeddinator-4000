using System;
using System.IO;

using Embeddinator;

namespace ObjC {
	
	public class ComparableHelper : MethodHelper {

		public ComparableHelper (SourceWriter headers, SourceWriter implementation) :
			base (headers, implementation)
		{
			ReturnType = "NSComparisonResult";
		}

		public void WriteImplementation ()
		{
			BeginImplementation ();
			WriteMethodLookup ();
			implementation.WriteLine ("return mono_embeddinator_compare_to (_object, __method, other ? other->_object : nil);");
			EndImplementation ();
		}
	}
}
