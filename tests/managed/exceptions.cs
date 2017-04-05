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
}