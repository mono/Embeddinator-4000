# Embeddinator-4000 0.4

What's New
==========

Objective-C support
-------------------

* [pr514](https://github.com/mono/Embeddinator-4000/pull/514) - Bitcode support added to iOS
* [pr540](https://github.com/mono/Embeddinator-4000/pull/540) - Extension support added to iOS

Java support
------------

* [pr542](https://github.com/mono/Embeddinator-4000/pull/542) - Improved overload generation for Java methods

Android support
---------------

* [pr525](https://github.com/mono/Embeddinator-4000/pull/525) - Update Xamarin.Android.Tools to latest
* [pr527](https://github.com/mono/Embeddinator-4000/pull/527) - Support for latest Android Studio 3.0
* [pr532](https://github.com/mono/Embeddinator-4000/pull/532) - Added missing Xamarin.Android `MonoPackageManager.setContext` method
* [pr541](https://github.com/mono/Embeddinator-4000/pull/541) - Updated to latest 15-4 stable branch of Xamarin.Android

Windows support
---------------

* [pr506](https://github.com/mono/Embeddinator-4000/pull/506) - Improved support for finding Mono SDK on Windows

Linux support
-------------

* [pr500](https://github.com/mono/Embeddinator-4000/pull/500) - Improved support for Linux platforms

Other features and fixes
------------------------

* [pr503](https://github.com/mono/Embeddinator-4000/pull/503) - We now provide an API for setting Mono runtime assembly paths

Known Issues
============

* There has been some work in developing a new Swift backend for Mac and iOS platforms, but it's not ready for prime time yet
* Some C# features (e.g. generics) are not yet supported;
* In some cases the generator produces duplicate symbols, which won't compile. Please file issues on github if this occurs.

A list of issues, including bugs, enhancements and ideas, is being tracked on [github](https://github.com/mono/Embeddinator-4000/issues).

