using Foundation;

namespace CustomUI
{
    public class MyObject : NSObject
    {
		[Export ("addStatic:to:")]
		public static int AddStatic (int a, int b)
		{
			return a + b;
		}

		[Export ("addInstance:to:")]
		public int AddInstance (int a, int b)
		{
			return a + b;
		}
	}
}
