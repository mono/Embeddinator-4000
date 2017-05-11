# Embeddinator-4000 0.1

This is the first public release of Embeddinator-4000. It supports the Objective-C language on both macOS or iOS. More languages and platforms will be added in future releases.


Requirements
============

* macOS 10.12 (Sierra) or later;
* Xcode 8.3.2 or later;
* Mono 5.0 or later;

Optional
--------

* [Xamarin Studio](https://developer.xamarin.com/guides/cross-platform/xamarin-studio/); or 
* [Visual Studio for Mac](https://www.visualstudio.com/vs/visual-studio-mac/);
* **Xamarin.iOS 10.11 preview** Available from the alpha channel of Xamarin Studio and Visual Studio for Mac


What's New
==========

**Everything!**

The Embeddinator-4000 (e4k) can create a native library (or framework) from a managed assembly. This is done by embedding the mono runtime along with generated glue code to call the public API exposed by the assembly.

Some of the supported .NET features are:

* Reference and value types;
* Interfaces, exposed as Objective-C protocols;
* Enums to `NS_ENUM` or `NS_OPTIONS`;
* Methods and properties;
* Extensions methods, exposed as Objective-C categories;
* Operators overloads;
* Default values (on methods and constructors);
* Special support for `System.String` as `NSString`, `byte[]` as `NSData`, other arrays to `NSArray`...

When using with [Xamarin.iOS](https://www.xamarin.com/platform#ios) it is possible to create applications that consume managed NSObject-subclasses, e.g. you can use a custom, managed `UIView` from an Objective-C application.

[Click to get started!](getting-started-objective-c.md)


Known Issues
============

* This release only generates Objective-C code;
* This release only targets macOS and iOS, the latter requires Xamarin.iOS (10.11+) to be installed;
* Some C# features (e.g. generics) are not yet supported;
* In some cases the generator produces duplicate symbols, which won't compile;

A list of issues, including bugs, enhancements and ideas, is being tracked on [github](https://github.com/mono/Embeddinator-4000/issues).
