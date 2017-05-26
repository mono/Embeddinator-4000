using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Embeddinator
{
	public static class Utils
	{
		public static bool RunProcess (string filename, string arguments, out int exitCode, out string stdout, bool capture_stderr = false)
		{
			Console.WriteLine ($"\t{filename} {arguments}");
			var sb = new StringBuilder ();
			var stdout_done = new System.Threading.ManualResetEvent (false);
			var stderr_done = new System.Threading.ManualResetEvent (false);
			using (var p = new Process ()) {
				p.StartInfo.FileName = filename;
				p.StartInfo.Arguments = arguments;
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.RedirectStandardOutput = true;
				p.StartInfo.RedirectStandardError = capture_stderr;
				p.OutputDataReceived += (sender, e) => {
					if (e.Data == null) {
						stdout_done.Set ();
					}
					else {
						lock (sb)
							sb.AppendLine (e.Data);
					}
				};
				if (capture_stderr) {
					p.ErrorDataReceived += (sender, e) => {
						if (e.Data == null) {
							stderr_done.Set ();
						}
						else {
							lock (sb)
								sb.AppendLine (e.Data);
						}
					};
				}
				p.Start ();
				p.BeginOutputReadLine ();
				if (capture_stderr)
					p.BeginErrorReadLine ();
				p.WaitForExit ();
				stdout_done.WaitOne (TimeSpan.FromSeconds (1));
				if (capture_stderr)
					stderr_done.WaitOne (TimeSpan.FromSeconds (1));
				stdout = sb.ToString ();
				exitCode = p.ExitCode;
				return exitCode == 0;
			}
		}

		public static int RunProcess (string filename, string arguments)
		{
			int exitCode;
			string output;
			RunProcess (filename, arguments, out exitCode, out output, capture_stderr: true);
			if (exitCode != 0)
				Console.WriteLine (output);
			return exitCode;
		}

		public static bool RunProcess (string filename, string arguments, out int exitCode)
		{
			exitCode = RunProcess (filename, arguments);
			return exitCode == 0;
		}

		public static bool Xcrun (StringBuilder options, out int exitCode)
		{
			return RunProcess ("xcrun", options.ToString (), out exitCode);
		}

		public static bool Lipo (System.Collections.Generic.List<string> inputs, string output, out int exitCode)
		{
			Directory.CreateDirectory (Path.GetDirectoryName (output));
			if (inputs.Count == 1) {
				File.Copy (inputs[0], output, true);
				exitCode = 0;
				return true;
			}
			else {
				var lipo_options = new StringBuilder ("lipo ");
				foreach (var file in inputs)
					lipo_options.Append (file).Append (" ");
				lipo_options.Append ("-create -output ");
				lipo_options.Append (Quote (output));
				return Xcrun (lipo_options, out exitCode);
			}
		}

		public static string Quote (string f)
		{
			if (f.IndexOf (' ') == -1 && f.IndexOf ('\'') == -1 && f.IndexOf (',') == -1 && f.IndexOf ('$') == -1)
				return f;

			var s = new StringBuilder ();

			s.Append ('"');
			foreach (var c in f) {
				if (c == '"' || c == '\\')
					s.Append ('\\');

				s.Append (c);
			}
			s.Append ('"');

			return s.ToString ();
		}


		// Mono.Unix can't create symlinks to relative paths, it insists on making the target a full path before creating the symlink.
		[DllImport ("libc", SetLastError = true)]
		static extern int symlink (string path1, string path2);

		[DllImport ("libc")]
		static extern int unlink (string pathname);

		public static void CreateSymlink (string file, string target)
		{
			unlink (file);
			var rv = symlink (target, file);
			if (rv != 0)
				throw ErrorHelper.CreateError (16, $"Could not create symlink '{file}' -> '{target}': error {Marshal.GetLastWin32Error ()}");
		}

		public static void FileCopyIfExists (string source, string target)
		{
			if (!File.Exists (source))
				return;
			File.Copy (source, target, true);
		}
	}
}
