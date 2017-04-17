using System;
using System.Collections.Generic;

// static type
public static class Platform {

	// static get-only property
	public static bool IsWindows {
		get {
			switch (Environment.OSVersion.Platform) {
			case PlatformID.Win32NT:
			case PlatformID.Win32S:
			case PlatformID.Win32Windows:
			case PlatformID.WinCE:
				return true;
			}
			return false;
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
	}
}
