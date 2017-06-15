# Embeddinator-4000 0.2

This is the second public release of Embeddinator-4000. It supports the Objective-C language on macOS, iOS, and tvOS. More languages and platforms will be added in future releases.

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

- tvOS support - [pr329](https://github.com/mono/Embeddinator-4000/pull/329)
- Improved behavior when generated code would duplicate other library or system selectors.

Objective-C Support
-------------------
* [pr284](https://github.com/mono/Embeddinator-4000/pull/284) - [interop] Added support for `IFormatProvider` as a Objective-C protocol;
* [pr299](https://github.com/mono/Embeddinator-4000/pull/299) - [interop] Added support for managed types implementing `IEquatable<T>`;
* [pr292](https://github.com/mono/Embeddinator-4000/pull/292) - [interop] `System.Decimal` is now mapped with Foundation's `NSDecimalNumber`
* [pr315](https://github.com/mono/Embeddinator-4000/pull/315) - [generator] Better names are generated when types are used
* [pr344](https://github.com/mono/Embeddinator-4000/pull/344) - [interop] Added support for conversion between `NSDate` and `DateTime`.
* [pr332](https://github.com/mono/Embeddinator-4000/pull/332) - [objc] Add decimal array ref support
* [pr327](https://github.com/mono/Embeddinator-4000/pull/327) - [objc] Add support for System.TimeSpan into generated projects

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

* [pr339](https://github.com/mono/Embeddinator-4000/pull/339) - [driver] The command line tool now support `nowarn:` to reduce the number of warnings (e.g. when binding an assembly that cannot be modified);
* [pr339](https://github.com/mono/Embeddinator-4000/pull/339) - [driver] The command line tool now support `warnaserror:` to ensure some warnings cannot be overlooked;

Known Issues
============

* This release only generates Objective-C code;
* This release only targets macOS, iOS, and tvOS - the latter requires Xamarin.iOS (10.11+) to be installed;
* Some C# features (e.g. generics) are not yet supported;
* In some cases the generator produces duplicate symbols, which won't compile. Please file issues on github if this occurs.

A list of issues, including bugs, enhancements and ideas, is being tracked on [github](https://github.com/mono/Embeddinator-4000/issues).

