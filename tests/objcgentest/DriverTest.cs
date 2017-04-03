using NUnit.Framework;
using System;
using System.IO;
using Embeddinator;

namespace DriverTest {

	[TestFixture]
	public class OptionsChecks {

		[Test]
		public void Output ()
		{
			Random rnd = new Random ();
			string valid = Path.Combine (Path.GetTempPath (), "output-" + rnd.Next ().ToString ());
			try {
				Driver.OutputDirectory = valid;
				Assert.That (Directory.Exists (valid), "valid");

				try {
					Driver.OutputDirectory = "/:output";
					Assert.Fail ("invalid");
				} catch (EmbeddinatorException ee) {
					Assert.True (ee.Error, "Error");
					Assert.That (ee.Code, Is.EqualTo (1), "Code");
				}
			} finally {
				Directory.Delete (valid);
			}
		}
	}
}