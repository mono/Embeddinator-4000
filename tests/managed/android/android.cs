﻿using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using Java.Interop;
using R = managedandroid.Resource;

namespace Android
{
    [Register("mono.embeddinator.android.ViewSubclass")]
    public class ViewSubclass : TextView
    {
        public ViewSubclass(Context context) : base(context) { }

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

            //SetContentView(R.Layout.hello);

            //TODO: temporary until resources are working
            var text = new TextView(this);
            text.Text = GetText();
            SetContentView(text);
        }

        [Export("getText")]
        public string GetText()
        {
            //TODO: temporary until resources are working
            return "Hello from C#!";

            //return Resources.GetString(R.String.hello);
        }
    }
}
