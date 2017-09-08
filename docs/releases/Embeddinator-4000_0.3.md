# Embeddinator-4000 0.3

This is the third public release of Embeddinator-4000.

What's New
==========

Embeddinator now has support for Windows and Linux, for C and Java generators, as well as targetting the Android platform.

Java support
------------

* [pr475](https://github.com/mono/Embeddinator-4000/pull/475) - Improved support for C# interfaces, with generation of Java abstract class proxies
* [pr490](https://github.com/mono/Embeddinator-4000/pull/490) - Improved support for desktop Java in Windows, macOS and Linux platforms

Android support
---------------

* [pr475](https://github.com/mono/Embeddinator-4000/pull/475) - Support for Android Studio 3.0
* [pr474](https://github.com/mono/Embeddinator-4000/pull/474) - Improved assemblies referencing for Android resources compilation
* [pr477](https://github.com/mono/Embeddinator-4000/pull/477) - JAR file dependencies improvements for Xamarin.Forms apps

C support
---------

* [pr497](https://github.com/mono/Embeddinator-4000/pull/497) - Improved support for generation of class type arrays
* Fixes for compilation of generated code with VS2017 toolchain

Other features and fixes
------------------------

* [pr499](https://github.com/mono/Embeddinator-4000/pull/499) - Support for Linux platform, including a new CI testing configuration
* [pr495](https://github.com/mono/Embeddinator-4000/pull/495) [pr498](https://github.com/mono/Embeddinator-4000/pull/489) - Improved support for F# language
* [pr471](https://github.com/mono/Embeddinator-4000/pull/471) - Fixes for MSBuild lookup on Windows

Known Issues
============

* Some C# features (e.g. generics) are not yet supported;
* In some cases the generator produces duplicate symbols, which won't compile. Please file issues on github if this occurs.

A list of issues, including bugs, enhancements and ideas, is being tracked on [github](https://github.com/mono/Embeddinator-4000/issues).

