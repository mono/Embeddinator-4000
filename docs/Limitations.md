# Limitations

This document explains the limitations of **embeddinator-4000** and, whenever possible, provides workarounds for them.

## General

### Use more than one embedded library in a project

It is not possible to have two mono runtimes co-exists inside the same application. This means you cannot use two different, embeddinator-4000-generated libaries inside the same application.

**Workaround:** You can use the generator to create a single library that includes several assemblies (from different projects).


## ObjC generated code

### Nullability

There is no metadata, in .NET, that tell us if a null reference is acceptable or not for an API. Most API will throw `ArgumentNullException` if they cannot cope with a `null` argument. This is problematic as ObjC handling of exceptions is something better avoided.

Since we cannot generate accurate nullability annotations in the header files and wish to minimize managed exceptions we default to non-null arguments (`NS_ASSUME_NONNULL_BEGIN`) and add some specific, when precision is possible, nullability annotations.
