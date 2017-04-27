using System;

using NUnit.Framework;

using Embeddinator;

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
				Assert.That (ee.Code, Is.EqualTo (code), "Code");
				Assert.That (ee.Message, Is.EqualTo (message), "Message");
			}
		}
	}
}
