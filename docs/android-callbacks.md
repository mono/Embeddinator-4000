# Callbacks on Android

Calling to Java from C# is somewhat a *risky business*. That is to say there is a *pattern* for callbacks from C# to Java; however, it is more complicated than we would like.

We'll cover the three options for doing callbacks that make the most sense for Java:
- Abstract classes
- Interfaces
- Virtual methods

## Abstract Classes

This is the easiest route for callbacks, so I would recommend using `abstract` if you are just trying to get a callback working in the simplest form.

Let's start with a C# class we would like Java to implement:
```csharp
[Register("mono.embeddinator.android.AbstractClass")]
public abstract class AbstractClass : Java.Lang.Object
{
    public AbstractClass() { }

    public AbstractClass(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) { }

    [Export("getText")]
    public abstract string GetText();
}
```

So let's identify the details that make this work:
- `[Register]` generates a nice package name in Java--you will get an auto-generated package name without it.
- Subclassing `Java.Lang.Object` signals Embeddinator to run the class through Xamarin.Android's Java generator.
- Empty constructor: is the one you want Java to be using.
- `(IntPtr, JniHandleOwnership)` constructor: is what Xamarin.Android will use for creating the C#-equivalent of Java objects.
- `[Export]` is just needed if you want to make the method name different in Java, since the Java world likes to use lower case methods.

Next let's make a C# method to test the scenario:
```csharp
[Register("mono.embeddinator.android.JavaCallbacks")]
public class JavaCallbacks : Java.Lang.Object
{
    [Export("abstractCallback")]
    public static string AbstractCallback(AbstractClass callback)
    {
        return callback.GetText();
    }
}
```
`JavaCallbacks` could be any class to test this, as long as it is a `Java.Lang.Object`.

Now, run Embeddinator on your .NET assembly to generate an AAR. See the [Getting Started guide](getting-started-java-android.md) for details.

After importing the AAR file into Android Studio, let's write a unit test:
```java
@Test
public void abstractCallback() throws Throwable {
    AbstractClass callback = new AbstractClass() {
        @Override
        public String getText() {
            return "Java";
        }
    };

    assertEquals("Java", callback.getText());
    assertEquals("Java", JavaCallbacks.abstractCallback(callback));
}
```
So we:
- Implemented the `AbstractClass` in Java with an anonymous type
- Made sure our instance returns `"Java"` from Java
- Made sure our instance returns `"Java"` from C#
- Added `throws Throwable`, since C# constructors are currently marked with `throws`

If we ran this unit test as-is, it would fail with an error such as:
```
System.NotSupportedException: Unable to find Invoker for type 'Android.AbstractClass'. Was it linked away?
```

What is missing here is an `Invoker` type. This is a subclass of `AbstractClass` that forwards C# calls to Java. If a Java object enters the C# world and the equivalent C# type is abstract, then Xamarin.Android automatically looks for a C# type with the suffix `Invoker` for use within C# code.

Xamarin.Android uses this `Invoker` pattern for Java binding projects among other things.

Here is our implementation of `AbstractClassInvoker`:
```csharp
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
```

There is quite a bit going on here, we:
- Added a class with the suffix `Invoker` that subclasses `AbstractClass`
- Added `class_ref` to hold the JNI reference to the Java class that subclasses our C# class
- Added `id_gettext` to hold the JNI reference to the Java `getText` method
- Included a `(IntPtr, JniHandleOwnership)` constructor
- Implemented `ThresholdType` and `ThresholdClass` as a requirement for Xamarin.Android to know details about the `Invoker`
- `GetText` needed to lookup the Java `getText` method with the appropriate JNI signature and call it
- `Dispose` is just needed to clear the reference to `class_ref`

After adding this class and generating a new AAR, our unit test passes. As you can see this pattern for callbacks is not *ideal*, but doable.

For details on Java interop, see the amazing [Xamarin.Android documentation](https://developer.xamarin.com/guides/android/advanced_topics/java_integration_overview/working_with_jni/) on this subject.

## Interfaces

Interfaces are much the same as abstract classes, except for one detail: Xamarin.Android does not generate Java for them. This is because before Embeddinator-4000, there are not many scenarios where Java would implement a C# interface.

Let's say we have the following C# interface:
```csharp
[Register("mono.embeddinator.android.IJavaCallback")]
public interface IJavaCallback : IJavaObject
{
    [Export("send")]
    void Send(string text);
}
```
`IJavaObject` signals Embeddinator that this is a Xamarin.Android interface, but otherwise this is exactly the same as an `abstract` class.

Since Xamarin.Android will not currently generate the Java code for this interface, add the following Java to your C# project:
```java
package mono.embeddinator.android;

public interface IJavaCallback {
    void send(String text);
}
```
You can place the file anywhere, but make sure to set its build action to `AndroidJavaSource`. This will signal Embeddinator to copy it to the proper directory to get compiled into your AAR file.

Next, the `Invoker` implementation will be quite the same:
```csharp
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
```

After generating an AAR file, in Android Studio we could write the following passing unit test:
```java
class ConcreteCallback implements IJavaCallback {
    public String text;
    @Override
    public void send(String text) {
        this.text = text;
    }
}

@Test
public void interfaceCallback() {
    ConcreteCallback callback = new ConcreteCallback();
    JavaCallbacks.interfaceCallback(callback, "Java");
    assertEquals("Java", callback.text);
}
```

## Virtual Methods

Overriding a `virtual` in Java is possible, but not a great experience.

Let's assume you have the following C# class:
```csharp
[Register("mono.embeddinator.android.VirtualClass")]
public class VirtualClass : Java.Lang.Object
{
    public VirtualClass() { }

    public VirtualClass(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) { }

    [Export("getText")]
    public virtual string GetText() { return "C#"; }
}
```

If you followed the `abstract` class example above, it would work except for one detail: _Xamarin.Android won't lookup the `Invoker`_.

To fix this, modify the C# class to be `abstract`:
```csharp
public abstract class VirtualClass : Java.Lang.Object
```
This is not ideal, but it gets this scenario working. Xamarin.Android will pick up the `VirtualClassInvoker` and Java can use `@Override` on the method.

## Callbacks in the Future

There are a couple of things we could to do improve these scenarios:

1. `throws Throwable` on C# constructors is fixed on this [PR](https://github.com/xamarin/java.interop/pull/170).
1. Make the Java generator in Xamarin.Android support interfaces.
    - This removes the need for adding Java source file with a build action of `AndroidJavaSource`.
1. Make a way for Xamarin.Android to load an `Invoker` for virtual classes.
    - This removes the need to mark the class in our `virtual` example `abstract`.
1. Generate `Invoker` classes for Embeddinator automatically
    - This is going to be complicated, but doable. Xamarin.Android is already doing something similar to this for Java binding projects.

There is alot of work to be done here, but these enhancements to Embeddinator are possible.

## Further Reading

* [Getting Started on Android](getting-started-java-android.md)
* [Preliminary Android Research](android-preliminary-research.md)
* [Embeddinator Limitations](Limitations.md)
* [Contributing to the open source project](Contributing.md)
* [Error codes and descriptions](errors.md)