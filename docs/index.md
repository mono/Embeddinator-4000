
Embeddinator-4000 is a tool that allows your existing .NET Code (C#,
F# and others) to be consumed from other programming languages and in
various different environments.

This means that if you have a .NET library that you want to use from
your existing iOS app, you can do that.   Or if you want to link it
with a native C++ library, you can also do that.   Or consume .NET
code from Java.

# Environments and Languages

The tool is both aware of the environment it will use, as well as the
language that will consume it.   For example, the iOS platform does
not allow just-in-time (JIT) compilation, so the embeddinator will
statically compile your .NET code into native code that can be used in
iOS.  Other environments do allow JIT compilation, and in those
enviroments, we opt to JIT compile.

It supports various language consumers, so it surfaces .NET code as
idiomatic code in the target language.   This is the list of supported 
languages at present:

* *Objective-C*: mapping .NET to idiomatic Objective-C APIs.
* *Java*: mapping .NET to idiomatic Java APIs.
* *C*: mapping .NET to an object-oriented like C APIs.

More languages will come later.

# Getting Started

To get started, check one of our guides for each of the currently
supported languages:

* [Objective-C](getting-started-objective-c): covers macOS and iOS.
* [Java](getting-started-java): covers macOS and Android.
* [C](getting-started-c): covers C language on desktop platforms.
