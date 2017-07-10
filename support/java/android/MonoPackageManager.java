package mono;

/* This is used if Application.Context is invoked from C#, see: https://github.com/xamarin/xamarin-android/blob/master/src/Mono.Android/Android.App/Application.cs */
public class MonoPackageManager {
    public static android.content.Context Context;
}