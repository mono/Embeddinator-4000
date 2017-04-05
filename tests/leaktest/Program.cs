using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

class LeakTester
{
	[DllImport ("libc", EntryPoint="isatty")]
	extern static int _isatty (int fd);
	static bool HasControllingTerminal ()
	{
		return _isatty (0) != 0 && _isatty (1) != 0 && _isatty (2) != 0;
	}

	static int Main (string [] args)
	{
		int rv = 0;

		if (args.Length < 1) {
			Console.WriteLine ("A command to execute must be specified");
			return 1;
		}

		// For now we only look in the current directory for libLeakCheckAtExit.dylib.
		// We might have to improve that if this tool grows.
		var libLeakCheckAtExit = Path.GetFullPath ("libLeakCheckAtExit.dylib");
		if (!File.Exists (libLeakCheckAtExit)) {
			Console.WriteLine ("Could not find libLeakCheckAtExit.dylib");
			return 1;
		}

		var ready_file = Path.GetFullPath (".stamp-ready"); // this file is removed when the test app is ready for leak check
		var done_file = Path.GetFullPath (".stamp-done"); // this file is removed when the leak check is complete, this means the test app can exit

		File.WriteAllText (ready_file, string.Empty);
		File.WriteAllText (done_file, string.Empty);

		Environment.SetEnvironmentVariable ("LEAK_READY_FILE", ready_file);
		Environment.SetEnvironmentVariable ("LEAK_DONE_FILE", done_file);

		using (var p = new Process ()) {
			p.StartInfo.FileName = args [0];
			var sb = new StringBuilder ();
			for (int i = 1; i < args.Length; i++)
				sb.Append (" \"").Append (args [i]).Append ("\"");
			p.StartInfo.Arguments = sb.ToString ();
			p.StartInfo.EnvironmentVariables.Add ("MallocStackLogging", "1");
			p.StartInfo.EnvironmentVariables.Add ("MallocScribble", "1");
			p.StartInfo.EnvironmentVariables.Add ("DYLD_INSERT_LIBRARIES", libLeakCheckAtExit);
			p.StartInfo.UseShellExecute = false;
			Console.WriteLine ("Executing: {0} {1}", p.StartInfo.FileName, p.StartInfo.Arguments);
			p.Start ();

			while (File.Exists (ready_file)) {
				Console.WriteLine ("Waiting for app to become ready for leak test...");
				Thread.Sleep (100);
				if (p.HasExited) {
					Console.WriteLine ("App crashed/exited, no leak check can be performed.");
					return 1;
				}
			}

			Console.WriteLine ("Performing leak test...");
			using (var leaks = new Process ()) {
				var sudo = !HasControllingTerminal ();
				leaks.StartInfo.FileName = sudo ? "sudo" : "xcrun";
				sb.Clear ();
				if (sudo)
					sb.Append ("--non-interactive xcrun ");
				sb.Append ($"leaks {p.Id}");
				sb.Append (" -exclude mono_save_seq_point_info"); // I haven't investigated this
				sb.Append (" -exclude create_internal_thread_object"); // I haven't investigated this
				sb.Append (" -exclude mono_thread_attach_internal"); // I haven't investigated this (and it seems random)
				sb.Append (" -exclude mono_thread_set_name_internal"); // I haven't investigated this (and it seems random)
				sb.Append (" -exclude mono_assembly_load_from_full"); // there's a leak in aot-runtime.c:check_usable if multiple error conditions
				leaks.StartInfo.Arguments = sb.ToString ();
				leaks.StartInfo.UseShellExecute = false;
				leaks.StartInfo.RedirectStandardOutput = true;
				leaks.StartInfo.RedirectStandardError = true;
				DataReceivedEventHandler process_output = (object sender, DataReceivedEventArgs ea) =>
				{
					var line = ea.Data;
					if (line == null)
						return;
					if (!line.StartsWith ("\tCall stack: ", StringComparison.Ordinal)) {
						Console.WriteLine (line);
						return;
					}
					line = line.Substring ("\tCall stack: ".Length);
					var frames = new List<string> (line.Split (new string [] { " | " }, StringSplitOptions.None));
					if (frames.Count > 1 && frames [1] == "0x1")
						frames.RemoveAt (1);
					frames.Reverse ();
					Console.WriteLine ("\tCall stack: ");
					foreach (var frame in frames)
						Console.WriteLine ($"\t\t{frame}");
				};
				leaks.ErrorDataReceived += process_output;
				leaks.OutputDataReceived += process_output;

				Console.WriteLine ("{0} {1}", leaks.StartInfo.FileName, leaks.StartInfo.Arguments);
				leaks.Start ();

				leaks.BeginErrorReadLine ();
				leaks.BeginOutputReadLine ();

				leaks.WaitForExit ();

				Console.WriteLine ("Done performing leak test, result: {0}", leaks.ExitCode);
				rv = leaks.ExitCode;
			}

			File.Delete (done_file);

			Console.WriteLine ("Waiting for app to terminate...");

			p.WaitForExit ();

			Console.WriteLine ($"Done with exit code {p.ExitCode}");
		}
		return rv;
	}
}
