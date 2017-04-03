using System;

public class ClassWithoutNamespace {

	public override string ToString ()
	{
		return base.ToString ();
	}
}

namespace First {

	public class ClassWithSingleNamespace {

		public override string ToString ()
		{
			return base.ToString ();
		}
	}
}

namespace First.Second {

	public class ClassWithNestedNamespace {

		public override string ToString ()
		{
			return base.ToString ();
		}
	}
}
