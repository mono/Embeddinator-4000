# General

The **Embeddinator-4000** project is under the Mono umbrella and most of the [contribution rules](http://www.mono-project.com/community/contributing/), tools and process are identical. Even if it is a bit out of date, the spirit of [Mono Contribution HowTo](http://www.mono-project.com/community/contributing/contribution-howto/) still applies, including the **DO WRITE TESTS!!!**

* Bug reporting: Issues and enhancement requests are presently tracked in [github](https://github.com/mono/Embeddinator-4000/issues) (not bugzilla).
* Chat: https://gitter.im/managed-interop

## C/Java

The work on the C and Java generators occurs in the [`master`](https://github.com/mono/Embeddinator-4000/tree/master) branch. Here are the steps to build it from our repository/branch:

```
> git clone https://github.com/mono/Embeddinator-4000.git
> cd Embeddinator-4000
> git branch master
> git pull && git submodule update --init --recursive
```

Then, if you prefer the command line, you can either:

`> msbuild Embeddinator-4000.sln`

or run the provided compilation shell script:

`> build.sh`

If you're more GUI oriented, you can open the solution file at `Embeddinator-4000.sln` and hit F5 to build it.

Once complete you should be able to run the tool with

```
$ mono build/lib/Debug/Embeddinator-4000.exe
```

Tests can be executed by running `make` from the `tests/common` directory.

Or running the `tests/RunTestsuite.sh` shell script.

### Cake

We will slowly be moving our build scripts to Cake. We only have a few build targets setup so far. You may also need to run `git submodule update --init` before attempting a build.

On OS X, you can setup your environment for Android by running a shell script:
```
./build.sh -t Generate-Android -v diagnostic
```
On Windows, in Powershell:
```
.\build.ps1 -t Generate-Android -v diagnostic
```
This will download a master build of Xamarin.Android and extract it into `/external/Xamarin.Android`. 

`Embeddinator-4000.exe` will be compiled to `build/lib/Release`. The Cake script will also run Embeddinator against a test assembly, so you can be sure your system is setup properly.

*NOTE: as soon as a preview build containing our changes for Xamarin.Android is available, we will update these instructions.*

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

The installer can be built by running `make installer` in the `objcgen` directory.

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
