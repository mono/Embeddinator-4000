# Getting started with Java

This is the getting started page for Java, which covers the basics for all supported platforms.

## Requirements

In order to use the embeddinator with Java you will need:
* Java 1.7 or later
* [Mono 5.0](http://www.mono-project.com/download/)

For Mac:
* Xcode 8.3.2 or later

For Windows:
* Visual Studio 2017 with C++ support
* Windows 10 SDK

For Android:
* Xamarin.Android 7.3.x or later
* Android Studio 2.3.2 or later

Optionally you can install [Xamarin Studio](https://developer.xamarin.com/guides/cross-platform/xamarin-studio/) or the new [Visual Studio for Mac](https://www.visualstudio.com/vs/visual-studio-mac/) to edit and compile your C# code. The rest of the getting started guide assume you'll be using **Visual Studio for Mac**.


Notes:

* Earlier versions of Xcode, Visual Studio, Xamarin.Android, Android Studio, and Mono _might_ work, but are untested and unsupported.

## Installation

There is not currently an installer with Java/Android support. For now you can build from source, see our [git repository](https://github.com/mono/Embeddinator-4000/) and the [contributing](Contributing.md) document for instructions.

## Platforms

Java is currently in a prototype state for macOS and Android.

The platform is selected by passing the `--platform=<platform>` command-line
argument to the embeddinator. Currently `macOS` and `Android` are supported. `Windows` will be coming soon, but _might_ currently work to some extent.

### macOS and Windows

For development, should be able to use any Java IDE that supports Java 1.7. You can even use Android Studio for this if desired, [see here](https://stackoverflow.com/questions/16626810/can-android-studio-be-used-to-run-standard-java-projects). You can use the JAR file output as you would any standard Java jar file.

### Android

Please make sure you are already set up to develop Android applications before trying to create one using the embeddinator. The [following instructions](getting-started-java-android.md) assume that you have already successfully built and deployed an Android application from your computer.

Android Studio is recommended for development, but other IDEs should work as long as there is support for the [AAR file format](https://developer.android.com/studio/projects/android-library.html). 

## Further Reading

* [Getting Started on Android](getting-started-java-android.md)
* [Preliminary Android Research](android-preliminary-research.md)
* [Embeddinator Limitations](Limitations.md)
* [Contributing to the open source project](Contributing.md)
* [Error codes and descriptions](errors.md)
