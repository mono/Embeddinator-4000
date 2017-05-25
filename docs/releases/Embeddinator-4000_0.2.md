# Embeddinator-4000 0.2

_work in progress_


Requirements
============

* macOS 10.12 (Sierra) or later;
* Xcode 8.3.2 or later;
* Mono 5.0 or later;

Optional
--------

* [Visual Studio for Mac](https://www.visualstudio.com/vs/visual-studio-mac/);
* [Xamarin.iOS 10.11.0.136 or later](https://jenkins.mono-project.com/view/Xamarin.MaciOS/job/xamarin-macios-builds-master/) **Links to preview builds**. Required for iOS, tvOS and watchOS support;
* [Xamarin.iOS 3.5.0.144 or later](https://jenkins.mono-project.com/view/Xamarin.MaciOS/job/xamarin-macios-builds-master/) **Links to preview builds**. Required for standalone macOS support;


What's New
==========

This second release builds upon our initial version previewed at //build 2017.
_to be completed_

Objective-C Support
-------------------

* [pr284](https://github.com/mono/Embeddinator-4000/pull/284) - [interop] Added support for `IFormatProvider` as a Objective-C protocol;
* [pr299](https://github.com/mono/Embeddinator-4000/pull/299) - [interop] Added support for managed types implementing `IEquatable<T>`;
* [pr292](https://github.com/mono/Embeddinator-4000/pull/292) - [interop] `System.Decimal` is now mapped with Foundation's `NSDecimalNumber`
* [pr315](https://github.com/mono/Embeddinator-4000/pull/315) - [generator] Better names are generated when types are used

iOS Support
-----------

* [pr305](https://github.com/mono/Embeddinator-4000/pull/305) - [mtouch] Native symbols stripping now supported with Xamarin.iOS in embeddinator mode
* [pr319](https://github.com/mono/Embeddinator-4000/pull/319) - [linker] Automatically create XML file to preserve anything required from SDK assemblies;

macOS Support
-------------

* [pr324](https://github.com/mono/Embeddinator-4000/pull/324) - [mmp] Support for native frameworks generated with Xamarin.Mac;
* [pr324](https://github.com/mono/Embeddinator-4000/pull/324) - [registrar] Support for subclassing managed subclasses of `NSObject` from Xamarin.Mac based assemblies;

note: macOS support _without Xamarin.Mac_ remains supported but requires a compatible version of Mono to be installed on the computer;

tvOS Support
------------

* [pr329](https://github.com/mono/Embeddinator-4000/pull/329) - Support for tvOS is now available. It requires Xamarin.iOS to be installed;

Other New Features
------------------

* [xxx]() - [driver] The command line tool now support `nowarn:` to reduce the number of warnings (e.g. when binding an assembly that cannot be modified);
* [xxx]() - [driver] The command line tool now support `warnaserror:` to ensure some warnings cannot be overlooked;

Known Issues
============

_to be updated_

* This release only generates Objective-C code;
* This release only targets macOS and iOS, the latter requires Xamarin.iOS (10.11+) to be installed;
* Some C# features (e.g. generics) are not yet supported;
* In some cases the generator produces duplicate symbols, which won't compile;

A list of issues, including bugs, enhancements and ideas, is being tracked on [github](https://github.com/mono/Embeddinator-4000/issues).

