using System;
namespace Renamed
{
	public sealed class BindingNameAttribute : System.Attribute {
	    public string Name;

	    public BindingNameAttribute (string name)
	    {
		    Name = name;
	    }
	}

	public class WithItemsRenamed
	{
		[BindingName ("MyCustomClass")]
		public bool Class { get; set; }
		
		[BindingName ("MyCustomHash")]
		public int Hash () => 42;
	}

}
