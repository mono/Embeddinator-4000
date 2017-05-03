using System;
using System.IO;
using System.Text;

namespace Embeddinator {
	
	public class SourceWriter : TextWriter {

		StringBuilder sb = new StringBuilder ();
		bool newline = true;

		public bool Enabled { get; set; } = true;
		public int Indent { get; set; }

		public override Encoding Encoding => Encoding.UTF8;

		void ApplyTabs ()
		{
			if (newline) {
				for (int i = 0; i < Indent; i++)
					sb.Append ('\t');
				newline = false;
			}
		}

		// minimum to override - see http://msdn.microsoft.com/en-us/library/system.io.textwriter.aspx
		public override void Write (char value)
		{
			if (!Enabled)
				return;
			ApplyTabs ();
			sb.Append (value);
		}

		public override void WriteLine ()
		{
			if (!Enabled)
				return;
			sb.AppendLine ();
			newline = true;
		}

		public override void WriteLine (string value)
		{
			if (!Enabled)
				return;
			ApplyTabs ();
			sb.AppendLine (value);
			newline = true;
		}

		public override void Write (string value)
		{
			if (!Enabled)
				return;
			ApplyTabs ();
			sb.Append (value);
		}

		public void WriteLineUnindented (string value)
		{
			if (!Enabled)
				return;
			sb.AppendLine (value);
			newline = true;
		}

		public override string ToString ()
		{
			return sb.ToString ();
		}
	}
}
