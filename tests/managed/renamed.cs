using System;
namespace Renamed
{
	public sealed class EmbeddinatorNameAttribute : System.Attribute {
		public string Name;
		public string Language;

		public EmbeddinatorNameAttribute (string name, string language = "ObjC")
		{
			Name = name;
			Language = language;
		}
	}

	public class WithItemsRenamed
	{
		[EmbeddinatorName ("MyCustomClass")]
		public bool Class { get; set; }
		
		[EmbeddinatorName ("MyCustomHash")]
		public int Hash () => 42;
	}

}
