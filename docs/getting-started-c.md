# Getting started with C

## Requirements

In order to use the embeddinator with C you'll need a Mac or Windows machine running:

* macOS 10.12 (Sierra) or later
* Xcode 8.3.2 or later

* Windows 7, 8, 10 or later
* Visual Studio 2013 or later

* [Mono](http://www.mono-project.com/download/)


## Installation

Your next step is to download and install the embeddinator on your machine.

Binary builds for the C and Java generators are still not available but are coming soon.

As an alternative it is possible to build it from our git repository, see the [contributing](Contributing.md) document for instructions.


## Generation

To generate C code, invoke the Embeddinator tool passing the right flags to target the C language:

### Windows:

```
$ build/lib/Debug/Embeddinator-4000.exe --gen=c --output=managed_c --platform=windows --compile managed.dll
```

Make sure the to call Embeddinator from a Visual Studio command shell specific to the Visual Studio version you're targetting. 

### macOS

```
$ mono build/lib/Debug/Embeddinator-4000.exe --gen=c --output=managed_c --platform=macos --compile managed.dll
```

### Output files

If all goes well, you will be presented with the following output:

```
Parsing assemblies...
    Parsed 'managed.dll'
Processing assemblies...
Generating binding code...
    Generated: managed.h
    Generated: managed.c
    Generated: mscorlib.h
    Generated: mscorlib.c
    Generated: embeddinator.h
    Generated: glib.c
    Generated: glib.h
    Generated: mono-support.h
    Generated: mono_embeddinator.c
    Generated: mono_embeddinator.h
```

Since the `--compile` flag was passed to the tool, Embeddinator should also have compiled the output files into a shared library, which you can find next to the generated files, a `libmanaged.dylib` file on macOS, and `managed.dll` on Windows.

To consume the shared library you can include the `managed.h` C header file, which provides the C declarations corresponding to the respective managed library APIs and link with the previously mentioned compiled shared library.

## Further Reading

* [Embeddinator Limitations](Limitations.md)
* [Contributing to the open source project](Contributing.md)
* [Error codes and descriptions](errors.md)
