# General

The **Embeddinator-4000** project is under the Mono umbrella and most of the [contribution rules](http://www.mono-project.com/community/contributing/), tools and process are identical. Even if it is a bit out of date, the spirit of [Mono Contribution HowTo](http://www.mono-project.com/community/contributing/contribution-howto/) still applies, including the **DO WRITE TESTS!!!**

* Bug reporting: Issues and enhancement requests are presently tracked in [github](https://github.com/mono/Embeddinator-4000/issues) (not bugzilla).
* Chat: https://gitter.im/managed-interop


## Objective-C

The work on the Objective-C generator occurs in the [`objc`](https://github.com/mono/Embeddinator-4000/tree/objc) branch. Here are the steps to build it from our repository/branch:

```
> git clone https://github.com/mono/Embeddinator-4000.git
> cd Embeddinator-4000
> git branch objc
> git pull && git submodule update --init --recursive
> nuget restore
> msbuild
```

Once complete you should be able to run the tool with

```
$ mono objcgen/bin/Debug/objcgen.exe
```

Tests can be executed by running `make` from the `tests/objc-cli` directory.

Installers can be built by running "make installer" in the objcgen directory.

### Specific Goals

In addition to mapping .NET APIs to Objective-C we aim to:

* Generate modern and pleasing APIs to use from Objective-C;
* Bridge similar concepts between .NET and Objective-C, e.g. convert interfaces into protocols;
* Increase interoperability with Foundation.framework, e.g. map all `System.String` to `NSString`;
* Expose extra types from .NET base class libraries (BCL) when required (and when no native alternative exists);


### Helpful links

* [Adopting Modern Objective-C](https://developer.apple.com/library/content/releasenotes/ObjectiveC/ModernizationObjC/AdoptingModernObjective-C/AdoptingModernObjective-C.html)

* [Naming Methods](https://developer.apple.com/library/content/documentation/Cocoa/Conceptual/CodingGuidelines/Articles/NamingMethods.html#//apple_ref/doc/uid/20001282-1001865-BCIBJEFG)

* [Automatic Reference Counting](http://clang.llvm.org/docs/AutomaticReferenceCounting.html)
