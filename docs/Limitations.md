# Limitations

This document explains the limitations of **embeddinator-4000** and, whenever possible, provides workarounds for them.

## General

### Use more than one embeded libary in a project

It is not possible to have two mono runtimes co-exists inside the same application. This means you cannot use two different, embeddinator-4000-generated libaries inside the same application.

**Workaround:** You can use the generator to create a single library that includes several assemblies (from different projects).


## ObjC generated code

