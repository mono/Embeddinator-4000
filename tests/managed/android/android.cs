using System;
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

            //For ResourceIdManager to pick it up, Resource.designer.dll must be loaded
            //TODO: put this code somewhere else...
            System.Reflection.Assembly.Load("Resource.designer");

            //For testing, print all loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Util.Log.Debug("E4K", "Found assembly: {0}", assembly.FullName);
            }

            SetContentView(R.Layout.hello);
        }

        [Export("getText")]
        public string GetText()
        {
            return Resources.GetString(R.String.hello);
        }
    }
}
