using Android.Widget;
using Android.Content;

namespace Android
{
    public class ViewSubclass : TextView
    {
        public ViewSubclass(Context context) : base(context) { }

        public string TextToApply
        {
            get;
            set;
        }

        public void Apply()
        {
            Text = TextToApply;
        }
    }
}
