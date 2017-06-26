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

        public string TextToApply
        {
            [Export]
            get;
            [Export]
            set;
        }

        [Export]
        public void Apply()
        {
            Text = TextToApply;
        }
    }
}
