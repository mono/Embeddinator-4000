# Getting started with Java

This is the getting started page for Java, which covers the basics for all supported platforms.

## Requirements

In order to use the embeddinator with Java you will need:
* Java 1.8 or later
* [Mono 5.0](http://www.mono-project.com/download/)

For Mac:
* Xcode 8.3.2 or later

For Windows:
* Visual Studio 2017 with C++ support
* Windows 10 SDK

For Android:
* Xamarin.Android 7.4.99 or later (build from [Jenkins](https://jenkins.mono-project.com/view/Xamarin.Android/job/xamarin-android/lastSuccessfulBuild/Azure/))
* [Android Studio 3.x](https://developer.android.com/studio/preview/index.html) with Java 1.8

Optionally you can install [Xamarin Studio](https://developer.xamarin.com/guides/cross-platform/xamarin-studio/) or the new [Visual Studio for Mac](https://www.visualstudio.com/vs/visual-studio-mac/) to edit and compile your C# code. The rest of the getting started guide assume you'll be using **Visual Studio for Mac**.


Notes:

* Earlier versions of Xcode, Visual Studio, Xamarin.Android, Android Studio, and Mono _might_ work, but are untested and unsupported.

## Installation

Embeddinator is currently available on [NuGet](https://www.nuget.org/packages/Embeddinator-4000/):
```
nuget install Embeddinator-4000
```
This will place `Embeddinator-4000.exe` into the `packages/Embeddinator-4000/tools` directory.

Additionally, you can build Embeddinator from source, see our [git repository](https://github.com/mono/Embeddinator-4000/) and the [contributing](Contributing.md) document for instructions.

## Platforms

Java is currently in a preview state for macOS, Windows, and Android.

The platform is selected by passing the `--platform=<platform>` command-line argument to the embeddinator. Currently `macOS`, `Windows`, and `Android` are supported.

### macOS and Windows

For development, should be able to use any Java IDE that supports Java 1.8. You can even use Android Studio for this if desired, [see here](https://stackoverflow.com/questions/16626810/can-android-studio-be-used-to-run-standard-java-projects). You can use the JAR file output as you would any standard Java jar file.

### Android

Please make sure you are already set up to develop Android applications before trying to create one using Embeddinator. The [following instructions](getting-started-java-android.md) assume that you have already successfully built and deployed an Android application from your computer.

Android Studio is recommended for development, but other IDEs should work as long as there is support for the [AAR file format](https://developer.android.com/studio/projects/android-library.html). 

## Further Reading

* [Getting Started on Android](getting-started-java-android.md)
* [Callbacks on Android](android-callbacks.md)
* [Preliminary Android Research](android-preliminary-research.md)
* [Embeddinator Limitations](Limitations.md)
* [Contributing to the open source project](Contributing.md)
* [Error codes and descriptions](errors.md)
