using System;

namespace Exceptions {

	public class Throwers {

		// objc: exceptions are, mostly, terminal but it's _ok_ for `init` to return `nil`
		public Throwers ()
		{
			throw new NotFiniteNumberException ();
		}
	}

	public class ThrowInStaticCtor {

		static ThrowInStaticCtor ()
		{
			throw new Exception ();
		}

		public ThrowInStaticCtor ()
		{
			// should not be callable
			// obj: init will return nil
			Console.WriteLine ("Should not be printed");
		}
	}

	public class Base {

		// no default .ctor
		// objc: so no `init`

		public Base (bool broken)
		{
			if (broken)
				throw new Exception ();
		}
	}

	public class Super : Base {
		// some case won't work - we must take care not to leak in such cases
		public Super (bool broken) : base (broken)
		{
		}
	}
}