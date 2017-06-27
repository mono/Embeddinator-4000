using Android.Content;
using Android.Runtime;
using Android.Widget;
using Java.Interop;

namespace Android
{
    [Register("mono.embeddinator.ViewSubclass")]
    public class ViewSubclass : TextView
    {
        public ViewSubclass(Context context) : base(context) { }

        [Export("apply")]
        public void Apply(string text)
        {
            Text = text;
        }
    }
}
