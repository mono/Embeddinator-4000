# Limitations

This document explains the limitations of **embeddinator-4000** and, whenever possible, provides workarounds for them.

## General

### Use more than one embedded library in a project

It is not possible to have two mono runtimes co-exists inside the same application. This means you cannot use two different, embeddinator-4000-generated libaries inside the same application.

**Workaround:** You can use the generator to create a single library that includes several assemblies (from different projects).

### Subclassing

The embeddinator ease the integration of the mono runtime inside applications by exposing a set of ready-to-use API for the target language and platform.

However this is not a two way integration, e.g. you cannot subclass a managed type and expect managed code to call back inside your native code, since your managed code is unaware of this co-existance.

Depending on your needs it might be possible to workaround parts of this limitation, e.g.

* your managed code can p/invoke into your native code. This requires customizing your managed code to allow customization from native code;

* use products like Xamarin.iOS and expose a managed library that would allow (ObjC in this case) to subclass some managed NSObject subclasses.


## ObjC generated code

### Nullability

There is no metadata, in .NET, that tell us if a null reference is acceptable or not for an API. Most API will throw `ArgumentNullException` if they cannot cope with a `null` argument. This is problematic as ObjC handling of exceptions is something better avoided.

Since we cannot generate accurate nullability annotations in the header files and wish to minimize managed exceptions we default to non-null arguments (`NS_ASSUME_NONNULL_BEGIN`) and add some specific, when precision is possible, nullability annotations.
