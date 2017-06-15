# Embeddinator-4000 0.3

This is the third public release of Embeddinator-4000. It supports the Objective-C language on macOS, iOS, and tvOS. More languages and platforms will be added in future releases.

Requirements
============

* macOS 10.12 (Sierra) or later;
* Xcode 8.3.2 or later;
* Mono 5.0 or later;

Optional
--------

* [Visual Studio for Mac](https://www.visualstudio.com/vs/visual-studio-mac/);
* [Xamarin.iOS 10.11.0.136 or later](https://jenkins.mono-project.com/view/Xamarin.MaciOS/job/xamarin-macios-builds-master/) **Links to preview builds**. Required for iOS and tvOS support;
* [Xamarin.Mac 3.5.0.144 or later](https://jenkins.mono-project.com/view/Xamarin.MaciOS/job/xamarin-macios-builds-master/) **Links to preview builds**. Required for standalone macOS support;


What's New
==========

Objective-C Support
-------------------


iOS Support
-----------


macOS Support
-------------


tvOS Support
------------

Other New Features
------------------

Known Issues
============

* This release only generates Objective-C code;
* This release only targets macOS, iOS, and tvOS - the latter requires Xamarin.iOS (10.11+) to be installed;
* Some C# features (e.g. generics) are not yet supported;
* In some cases the generator produces duplicate symbols, which won't compile. Please file issues on github if this occurs.

A list of issues, including bugs, enhancements and ideas, is being tracked on [github](https://github.com/mono/Embeddinator-4000/issues).

