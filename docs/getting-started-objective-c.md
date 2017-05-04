# Getting started with Objective-C

This is the getting started page for Objective-C


## Requirements

In order to use the embeddinator with Objective-C you'll need a Mac computer running:

* macOS 10.12 (Sierra) or later
* Xcode 8.3.2 or later
* [Mono 5.0](http://www.mono-project.com/download/beta/) _presently in beta_

Optionally you can install [Xamarin Studio](https://developer.xamarin.com/guides/cross-platform/xamarin-studio/) or the new [Visual Studio for Mac](https://www.visualstudio.com/vs/visual-studio-mac/) to edit and compile your C# code. The rest of the getting started guide assume you'll be using **Visual Studio for Mac**.


Notes:

* Earlier versions of macOS, Xcode and Mono _might_ work, but are untested and unsupported;
* Code generation can be done on Windows, but it is only possible to compile it on a Mac computer where Xcode is installed;


## Installation

Your next step is to download and install the embeddinator on your Mac.

**add link**

There is no package to download yet. Until one is available you need to build it from our git repository, see the [constributing](Contributing#Objective-C) document for instructions.

Installers can be built by running "make installer" in the objcgen directory.

Once installed via an installer, you can run the tool via /Library/Frameworks/Xamarin.Embeddinator-4000/Commands/objcgen 

## Platforms

Objective-C is a language that is most commonly used to write applications for macOS, iOS, tvOS and watchOS; and the embeddinator supports all of those platforms. Working with each platform implies some key differences and is best explained separately.

### macOS

[Creating a macOS application](getting-started-objc-macos) is easiest since it does not involve as many additional steps, like setting up identity, provisining profiles, simulators and devices. You are encouraged to start with the macOS document before the one for iOS.

### iOS

Please make sure you are already set up to develop iOS applications before trying to create one using the embeddinator. The [following instructions](getting-started-objc-ios) assume that you have already successfully built and deployed an iOS application from your computer.

Note: Working for tvOS or watchOS is very similar to iOS. You should start with the iOS instructions and then apply them to your platform of choice.
