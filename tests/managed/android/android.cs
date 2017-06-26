using Android.Widget;
using Android.Content;
using Java.Interop;

namespace Android
{
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
