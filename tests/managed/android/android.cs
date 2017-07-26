﻿using System;
using System.Net;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Util;
using Android.Widget;
using Java.Interop;
using R = managedandroid.Resource;

[assembly: UsesPermission("android.permission.INTERNET")]

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

    [Register("mono.embeddinator.android.AndroidAssertions")]
    public class AndroidAssertions : Java.Lang.Object
    {
        [Export("applicationContext")]
        public static Context ApplicationContext()
        {
            return Application.Context;
        }

        [Export("asyncAwait")]
        public static async void AsyncAwait()
        {
            var looper = Looper.MyLooper();

            await Task.Delay(1);

            if (looper != Looper.MyLooper())
                throw new Exception("We should be on the same thread!");
        }

        [Export("webRequest")]
        public static string WebRequest()
        {
            using (var client = new WebClient())
                return client.DownloadString("https://www.google.com");
        }

        [Export("callIntoSupportLibrary")]
        public static LocalBroadcastManager CallIntoSupportLibrary()
        {
            return LocalBroadcastManager.GetInstance(Application.Context);
        }
    }

    [Register("mono.embeddinator.android.JavaCallbacks")]
    public class JavaCallbacks : Java.Lang.Object
    {
        [Export("virtualCallback")]
        public static string VirtualCallback(VirtualClass callback)
        {
            return callback.GetText();
        }

        [Export("abstractCallback")]
        public static string AbstractCallback(AbstractClass callback)
        {
            return callback.GetText();
        }

        [Export("interfaceCallback")]
        public static void InterfaceCallback(IJavaCallback callback, string text)
        {
            callback.Send(text);
        }
    }

    /// <summary>
    /// TODO: marking this class "abstract" is not desired
    /// </summary>
    [Register("mono.embeddinator.android.VirtualClass")]
    public abstract class VirtualClass : Java.Lang.Object
    {
        public VirtualClass() { }

        public VirtualClass(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) { }

        [Export("getText")]
        public virtual string GetText() { return "C#"; }
    }

    class VirtualClassInvoker : VirtualClass
    {
        IntPtr class_ref, id_gettext;

        public VirtualClassInvoker(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
            IntPtr lref = JNIEnv.GetObjectClass(Handle);
            class_ref = JNIEnv.NewGlobalRef(lref);
            JNIEnv.DeleteLocalRef(lref);
        }

        protected override Type ThresholdType
        {
            get { return typeof(VirtualClassInvoker); }
        }

        protected override IntPtr ThresholdClass
        {
            get { return class_ref; }
        }

        public override string GetText()
        {
            if (id_gettext == IntPtr.Zero)
                id_gettext = JNIEnv.GetMethodID(class_ref, "getText", "()Ljava/lang/String;");
            IntPtr lref = JNIEnv.CallObjectMethod(Handle, id_gettext);
            return GetObject<Java.Lang.String>(lref, JniHandleOwnership.TransferLocalRef)?.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            if (class_ref != IntPtr.Zero)
                JNIEnv.DeleteGlobalRef(class_ref);
            class_ref = IntPtr.Zero;

            base.Dispose(disposing);
        }
    }

    [Register("mono.embeddinator.android.AbstractClass")]
    public abstract class AbstractClass : Java.Lang.Object
    {
        public AbstractClass() { }

        public AbstractClass(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) { }

        [Export("getText")]
        public abstract string GetText();
    }

    class AbstractClassInvoker : AbstractClass
    {
        IntPtr class_ref, id_gettext;

        public AbstractClassInvoker(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
            IntPtr lref = JNIEnv.GetObjectClass(Handle);
            class_ref = JNIEnv.NewGlobalRef(lref);
            JNIEnv.DeleteLocalRef(lref);
        }

        protected override Type ThresholdType
        {
            get { return typeof(AbstractClassInvoker); }
        }

        protected override IntPtr ThresholdClass
        {
            get { return class_ref; }
        }

        public override string GetText()
        {
            if (id_gettext == IntPtr.Zero)
                id_gettext = JNIEnv.GetMethodID(class_ref, "getText", "()Ljava/lang/String;");
            IntPtr lref = JNIEnv.CallObjectMethod(Handle, id_gettext);
            return GetObject<Java.Lang.String>(lref, JniHandleOwnership.TransferLocalRef)?.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            if (class_ref != IntPtr.Zero)
                JNIEnv.DeleteGlobalRef(class_ref);
            class_ref = IntPtr.Zero;

            base.Dispose(disposing);
        }
    }

    [Register("mono.embeddinator.android.IJavaCallback", DoNotGenerateAcw = true)]
    public interface IJavaCallback : IJavaObject
    {
        [Export("send")]
        void Send(string text);
    }

    class IJavaCallbackInvoker : Java.Lang.Object, IJavaCallback
    {
        IntPtr class_ref, id_send;

        public IJavaCallbackInvoker(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
            IntPtr lref = JNIEnv.GetObjectClass(Handle);
            class_ref = JNIEnv.NewGlobalRef(lref);
            JNIEnv.DeleteLocalRef(lref);
        }

        protected override Type ThresholdType
        {
            get { return typeof(IJavaCallbackInvoker); }
        }

        protected override IntPtr ThresholdClass
        {
            get { return class_ref; }
        }

        public void Send(string text)
        {
            if (id_send == IntPtr.Zero)
                id_send = JNIEnv.GetMethodID(class_ref, "send", "(Ljava/lang/String;)V");
            JNIEnv.CallVoidMethod(Handle, id_send, new JValue(new Java.Lang.String(text)));
        }

        protected override void Dispose(bool disposing)
        {
            if (class_ref != IntPtr.Zero)
                JNIEnv.DeleteGlobalRef(class_ref);
            class_ref = IntPtr.Zero;

            base.Dispose(disposing);
        }
    }

    [Register("mono.embeddinator.android.SQLite")]
    public class SQLite : Java.Lang.Object
    {
        [Export("connect")]
        public static int Connect()
        {
            //All these Init() ensure the linker doesn't strip assemblies we need
            SQLitePCL.Batteries_V2.Init();
            SQLitePCL.lib.embedded.Init();

            //Sets the provider to load libe_sqlite3.so for SQLite
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());

            SQLitePCL.sqlite3 db = null;
            try
            {
                //Open an in-memory db, and just return the result code, 0 is OK
                return SQLitePCL.raw.sqlite3_open(":memory:", out db);
            }
            finally
            {
                if (db != null)
                    db.Dispose();
            }
        }
    }

    [Register("mono.embeddinator.android.Resources")]
    public class Resources : Java.Lang.Object
    {
        public static string Hello
        {
            [Export("getHello")]
            get { return Application.Context.GetString(R.String.hello);  }
        }

        public static string LibraryName
        {
            [Export("getLibraryName")]
            get { return Application.Context.GetString(R.String.library_name); }
        }

        public static string ApplicationName
        {
            [Export("getApplicationName")]
            get { return Application.Context.GetString(R.String.applicationName); }
        }

        public static string ThisIsCaps
        {
            [Export("getThisIsCaps")]
            get { return Application.Context.GetString(R.String.THIS_IS_CAPS); }
        }
    }
}
