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

namespace First.Second.Third {

	// same class name as `First.Second.ClassWithNestedNamespace` but it's a different type
	public class ClassWithNestedNamespace {

		public override string ToString ()
		{
			return base.ToString ();
		}
	}
}
