using System;

// static type
public static class Platform {

	// static get-only property
	public static bool IsWindows {
		get {
#if PCL || NETSTANDARD1_6
			//NOTE: maybe there is something more precise than this
			return Environment.NewLine == "\r\n";
#else
			switch (Environment.OSVersion.Platform) {
			case PlatformID.Win32NT:
			case PlatformID.Win32S:
			case PlatformID.Win32Windows:
			case PlatformID.WinCE:
				return true;
			}
			return false;
#endif
		}
	}
	
	public static int ExitCode { get; set; }

	// not generated
	internal static string Text { get; set; }

	// not generated
	private static object Null {
		get { return null; }
	}
}

namespace Properties {

	public class Query {

		// static
		public static int UniversalAnswer { get; } = 42;

		public bool IsGood { get; } = true;
		public bool IsBad { get; } = false;

		public int Answer { get; set; } = UniversalAnswer;

		int secret;

		// setter only properties are valid (even if discouraged) in .net
		public int Secret {
			set { secret = value; }
		}

		public bool IsSecret {
			get { return secret != 0; }
		}

		// conflict between property get() and get method
		public Query Foo => new Query();
		public Query GetFoo() => Foo;
		public Query Get_Foo() => Foo;
		public Query GeT_Foo() => Foo;

		// conflict between property set() and set method
		public Query Bar { get; set; }
		public Query SetBar() => Bar;
	}

	public class DuplicateIndexedProperties {
		public int this[int i] { get { return 42; } }
		public int this[string i] { get { return 42; } }
	}
}
