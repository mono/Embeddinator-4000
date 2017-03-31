using System;

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
}
