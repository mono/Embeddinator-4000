using System;
using System.Collections.Generic;

namespace Embeddinator.ObjC
{
	public static class Logger
	{
		public static void Log (string line) => Log (1, line);

#if DEBUG
		static List<string> Entries = new List<string> ();
#endif

		public static void Log (int verbosityRequired, string line)
		{
			if (ErrorHelper.Verbosity >= verbosityRequired)
				Console.WriteLine (line);
#if DEBUG
			Entries.Add (line);
#endif
		}

		public static void Dump ()
		{
#if DEBUG
			Entries.ForEach (l => Console.WriteLine (l));
#endif
		}
	}
}
