using System;

using NUnit.Framework;

using Embeddinator.ObjC;
using System.IO;

namespace DriverTest
{
	public static class Asserts
	{
		public static void ThrowsEmbeddinatorException (int code, Action action)
		{
			try {
				action ();
				Assert.Fail ($"Expected EM{code} exception, but no exception was thrown.");
			} catch (EmbeddinatorException ee) {
				Assert.That (ee.Error, "Error");
				Assert.That (ee.Code, Is.EqualTo (code), "Code");
			}
		}
		public static void ThrowsEmbeddinatorException (int code, string message, Action action)
		{
			try {
				action ();
				Assert.Fail ($"Expected EM{code} exception, but no exception was thrown.", message);
			} catch (EmbeddinatorException ee) {
				Assert.That (ee.Error, "Error");
				Assert.That (ee.Code, Is.EqualTo (code), "Code - " + ee.Message);
				Assert.That (ee.Message, Is.EqualTo (message), "Message");
			}
		}

		public static void RunProcess (string filename, string arguments, string message)
		{
			string stdout;
			RunProcess (filename, arguments, out stdout, message);
		}

		public static void RunProcess (string filename, string arguments, out string stdout, string message)
		{
			int exitCode;
			Console.WriteLine ($"{filename} {arguments}");
			// We capture stderr too, otherwise it won't show up in the test unit pad's output.
			if (Utils.RunProcess (filename, arguments, out exitCode, out stdout, capture_stderr: true))
				return;
			Console.WriteLine ($"Command failed with exit code: {exitCode}");
			Console.WriteLine (stdout);
			Console.WriteLine ($"Command failed with exit code: {exitCode}");
			Assert.Fail ($"Executing '{filename} {arguments}' failed with exit code {exitCode}: {message}");
		}

		static string RunRedirectOutput (Action action)
		{
			using (MemoryStream memory = new MemoryStream ()) {
				using (var outputStream = new StreamWriter (memory)) {
					Console.SetOut (outputStream);
					Console.SetError (outputStream);
					action ();
					return System.Text.Encoding.UTF8.GetString (memory.ToArray ());
				}
			}
		}

		public static void Generate (string message, params string [] arguments)
		{
			int rv = 0;
			string output = RunRedirectOutput (() => {
				rv = Driver.Main2 (arguments);
			});

			if (rv == 0)
				return;
			Assert.Fail ($"Generation failed with exit code {rv}: {message}\n{output}");
		}
	}
}
