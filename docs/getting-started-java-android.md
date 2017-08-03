# Getting started with Android

In addition to the requirements from our [Getting started with Java](getting-started-java.md) guide you'll also need:

* Xamarin.Android 7.4.99 or later (build from [Jenkins](https://jenkins.mono-project.com/view/Xamarin.Android/job/xamarin-android/lastSuccessfulBuild/Azure/))
* Android Studio 2.3.x or later (with [Java 1.8](https://developer.android.com/guide/platform/j8-jack.html))

*NOTE: the state of using Java 1.8 in Android Studio is currently in [flux](https://android-developers.googleblog.com/2017/03/future-of-java-8-language-feature.html) at the moment. At the time of writing, the best option is to enable the Jack toolchain in the stable version of Android Studio. Details below.*

As an overview, we will:
* Create a C# Android Library project
* Install Embeddinator-4000 via NuGet
* Run Embeddinator on the Android library assembly
* Use the generated AAR file in a Java project in Android Studio

## Create an Android Library Project

Open Visual Studio for Windows or Mac, create a new Android Class Library project, name it `hello-from-csharp`, and save it to `~/Projects/hello-from-csharp` or `%USERPROFILE%\Projects\hello-from-csharp`.

Add a new Android Activity named `HelloActivity.cs`, followed by an Android Layout at `Resource/layout/hello.axml`.

Add a new `TextView` to your layout, and change the text to something enjoyable.

Your layout source should look something like this:
```xml
<?xml version="1.0" encoding="utf-8"?>
<LinearLayout xmlns:android="http://schemas.android.com/apk/res/android"
    android:orientation="vertical"
    android:layout_width="match_parent"
    android:layout_height="match_parent"
    android:minWidth="25px"
    android:minHeight="25px">
    <TextView
        android:text="Hello from C#!"
        android:layout_width="match_parent"
        android:layout_height="wrap_content"
        android:gravity="center" />
</LinearLayout>
```

In your activity, make sure you are calling `SetContentView` with your new layout:
```csharp
[Activity(Label = "HelloActivity"), 
    Register("hello_from_csharp.HelloActivity")]
public class HelloActivity : Activity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        SetContentView(Resource.Layout.hello);
    }
}
```
*NOTE: don't forget the `[Register]` attribute, see details under Limitations*

Build the project, the resulting assembly will be saved in `bin/Debug/hello-from-csharp.dll`.

## Installing Embeddinator-4000 from NuGet

Choose _Add | Add NuGet Packages..._ and install `Embeddinator-4000` from the NuGet package manager:
![NuGet Package Manager](VisualStudioNuGet.png)

This will install `Embeddinator-4000.exe` into the `packages/Embeddinator-4000/tools` directory.

## Run Embeddinator-4000

We will add a post-build step to run Embeddinator and create a native AAR file for the Android library project assembly.

In Visual Studio for Mac, go to _Project Options | Build | Custom Commands_ and add an _After Build_ step.

Setup the following commnd:
```
mono '${SolutionDir}/packages/Embeddinator-4000.0.2.0.80/tools/Embeddinator-4000.exe' '${TargetPath}' --gen=Java --platform=Android --outdir='${SolutionDir}/output' -c
```
_NOTE: make sure to use the version number you installed from NuGet_

If you are going to be doing ongoing development on the C# project, you might also add a custom command to clean the `output` directory prior to running Embeddinator:
```
rm -Rf '${SolutionDir}/output/'
```

![Custom Build Action](VisualStudioCustomBuild.png)

The Android AAR file will be placed in `~/Projects/hello-from-csharp/output/hello_from_csharp.aar`. _NOTE: hyphens are replaced because Java does not support it in package names._

### Running Embeddinator on Windows

We will essentially setup the same thing, but the menus in Visual Studio are a bit different on Windows. The shell commands are also slightly different.

Go to _Project Options | Build Events_ and enter the following into the _Post-build event command line_ box:
```
set E4K_OUTPUT="$(SolutionDir)output"
if exist %E4K_OUTPUT% rmdir /S /Q %E4K_OUTPUT%
"$(SolutionDir)packages\Embeddinator-4000.0.2.0.80\tools\Embeddinator-4000.exe" "$(TargetPath)" --gen=Java --platform=Android --outdir=%E4K_OUTPUT% -c
```

Such as the following screenshot:

![Embeddinator on Windows](VisualStudioWindows.png)

## Use the generated output in an Android Studio project

Open Android Studio and create a new project with an `Empty Activity`.

Right-click on your `app` module and choose `New | Module`. Select `Import .JAR/.AAR Package`. Use the directory browser to locate `~/Projects/hello-from-csharp/output/hello_from_csharp.aar` and hit `Finish`.

![Import AAR into Android Studio](AndroidStudioImport.png)

This will copy the AAR file into a new module named `hello_from_csharp`.

![Android Studio Project](AndroidStudioProject.png)

To use the new module from your `app`, right-click and choose `Open Module Settings`. On the `Dependencies` tab, add a new `Module Dependency` and choose `:hello_from_csharp`.

![Android Studio Dependencies](AndroidStudioDependencies.png)

In your activity, add a new `onResume` method, and let's do the easiest thing to launch our C# activity:
```java
import hello_from_csharp.*;

public class MainActivity extends AppCompatActivity {
    //... Other stuff here ...
    @Override
    protected void onResume() {
        super.onResume();

        Intent intent = new Intent(this, HelloActivity.class);
        startActivity(intent);
    }
}
```

### Assembly Compression *IMPORTANT*
One further change is required for Embeddinator in your Android Studio project.

Open your app's `build.gradle` file and add the following change:
```groovy
android {
    // ...
    aaptOptions {
        noCompress 'dll'
    }
}
```
Xamarin.Android currently loads .NET assemblies directly from the APK, but it requires the assemblies to not be compressed.

If you do not have this setup, the app will crash on launch and print something like this to the console:
```
com.xamarin.hellocsharp A/monodroid: No assemblies found in '(null)' or '<unavailable>'. Assuming this is part of Fast Deployment. Exiting...
```

## Run the app
Upon launching your app:

![Hello from C# sample running in the emulator](hello-from-csharp-android.png)

Note what happened here:
* We have a C# class, `HelloActivity`, that subclasses Java
* We have Android Resource files
* We used these from Java in Android Studio

So for this sample to work, all the following are setup in the final APK:
* Xamarin.Android is configured on application start
* .NET assemblies included in `assets/assemblies`
* `AndroidManifest.xml` modifications for your C# activities, etc.
* Android Resources and Assets from .NET libraries
* [Android Callable Wrappers](https://developer.xamarin.com/guides/android/advanced_topics/java_integration_overview/android_callable_wrappers/) for any `Java.Lang.Object` subclass

If you are looking for an additional walkthrough, check out this video embedding Charles Petzold's [FingerPaint demo](https://developer.xamarin.com/samples/monodroid/ApplicationFundamentals/FingerPaint/) in an Android Studio project here:

[![Embeddinator-4000 for Android](https://img.youtube.com/vi/ZVcrXUpCNpI/0.jpg)](https://www.youtube.com/watch?v=ZVcrXUpCNpI)

## Using Java 1.8

As of writing this, the best option is to use Android Studio 2.3.x stable and enable the Jack toolchain.

So in your app module's `build.gradle` file:
```groovy
android {
    // ..
    defaultConfig {
        // ...
        jackOptions.enabled true
    }
    // ...
    compileOptions {
        sourceCompatibility JavaVersion.VERSION_1_8
        targetCompatibility JavaVersion.VERSION_1_8
    }
}
```
You can also take a look at our Android Studio test project [here](https://github.com/mono/Embeddinator-4000/blob/master/tests/android/app/build.gradle) for more details.

Eventually Android Studio 3.0 should be the preferred option; however, Android Studio 3.0 currently has the limitation of not being able to use local AAR files. See an issue on this [here](https://github.com/mono/Embeddinator-4000/issues/448).

## Current Limitations on Android

Right now if you subclass `Java.Lang.Object`, Xamarin.Android will generate the Java stub (Android Callable Wrapper) instead of Embeddinator.

So you must follow the same rules for exporting C# to Java as Xamarin.Android. 

So for example in C#:
```csharp
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
```

* `[Register]` is required to map to a desired Java package name
* `[Export]` is required to make a method visible to Java

We can use `ViewSubclass` in Java like so:
```java
import mono.embeddinator.android.ViewSubclass;
//...
ViewSubclass v = new ViewSubclass(this);
v.apply("Hello");
```

Read more about Java integration with Xamarin.Android [here](https://developer.xamarin.com/guides/android/advanced_topics/java_integration_overview/).

## Multiple Assemblies

Embedding a single assembly is straightforward; however, it is much more likely you will have more than one C# assembly. Many times you will have dependencies on NuGet packages such as the Android support libraries or Google Play Services that further complicate things.

This causes a dilemma, since Embeddinator needs to include many types of files into the final AAR such as:
* Android assets
* Android resources
* Android native libraries
* Android java source

You most likely do not want to include these files from the Android support library or Google Play Services into your AAR, but would rather use the official version from Google in Android Studio.

Here is the recommended approach:
* Pass Embeddinator any assembly that you own (have source for) and want to call from Java
* Pass Embeddinator any assembly that you need Android assets, native libraries, or resources from
* Add Java dependencies like the Android support library or Google Play Services in Android Studio

So your command might be:
```
mono MonoEmbeddinator4000.exe --gen=Java --platform=Android -c -o output YourMainAssembly.dll YourDependencyA.dll YourDependencyB.dll
```
You should exclude anything from NuGet, unless you find out it contains Android assets, resources, etc. that you will need in your Android Studio project. You can also omit dependencies that you do not need to call from Java, and the linker _should_ include the parts of your library that are needed.

To add any Java dependencies needed in Android Studio, your `build.gradle` file might look like:
```groovy
dependencies {
    // ...
    compile 'com.android.support:appcompat-v7:25.3.1'
    compile 'com.google.android.gms:play-services-games:11.0.4'
    // ...
}
```

## Further Reading

* [Callbacks on Android](android-callbacks.md)
* [Preliminary Android Research](android-preliminary-research.md)
* [Embeddinator Limitations](Limitations.md)
* [Contributing to the open source project](Contributing.md)
* [Error codes and descriptions](errors.md)