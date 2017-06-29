using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Widget;
using Java.Interop;
using R = managedandroid.Resource;

namespace Android
{
    [Register("mono.embeddinator.android.ViewSubclass")]
    public class ViewSubclass : TextView
    {
        public ViewSubclass(Context context) : base(context) { }

        /// <summary>
        /// NOTE: this ctor tests theming
        /// </summary>
        public ViewSubclass(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            var a = context.ObtainStyledAttributes(attrs, R.Styleable.Theme);
            Text = a.GetString(R.Styleable.Theme_hello);
        }

        [Export("apply")]
        public void Apply(string text)
        {
            Text = text;
        }
    }

    [Register("mono.embeddinator.android.ButtonSubclass")]
    public class ButtonSubclass : Button
    {
        public ButtonSubclass(Context context) : base(context)
        {
            Click += (sender, e) => Times++;
        }

        public int Times
        {
            [Export("getTimes")]
            get;
            private set;
        }
    }

    [Activity(Label = "Activity Subclass"), Register("mono.embeddinator.android.ActivitySubclass")]
    public class ActivitySubclass : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(R.Layout.hello);
        }

        [Export("getText")]
        public string GetText()
        {
            return Resources.GetString(R.String.hello);
        }
    }
}
