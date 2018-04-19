![Embeddinator-4000 Logo](e4000-logo.png)

| Windows                   | macOS                       |
|---------------------------|-----------------------------|
| [![windows-vs-x86][1]][2] | [![osx-clang-x86][3]][4]

[1]: https://ci.appveyor.com/api/projects/status/lnmi5dh2ukm1n79o/branch/master?svg=true
[2]: https://ci.appveyor.com/project/tritao/embeddinator-4000/branch/master
[3]: https://travis-ci.org/mono/Embeddinator-4000.svg?branch=master
[4]: https://travis-ci.org/mono/Embeddinator-4000

Embeddinator-4000 is a tool to turn existing .NET libraries into
libraries that can be consumed by other languages.   

It is a tool that takes a .NET assembly and generates the necessary
glue to surface the .NET API as a native API.   The goal is to surface
.NET libraries to all ecosystems where Mono/Xamarin run, and for each
platform we provide an interface that is native to that platform as well
as the tools needed to turn a .NET library into something that can be 
consumed on that platform.

Presently there is support for .NET to C, Objective-C (across the various Apple platforms)
and Java (Android and regular Java), across Windows, Linux and macOS platforms.

## Getting Started

Check out our [documentation to get started](https://docs.microsoft.com/en-us/xamarin/tools/dotnet-embedding/index).

## Community

Feel free to join us at our [#managed-interop](https://gitter.im/managed-interop) Gitter discussion channel.

## Building

- Clone this repository 
- Initialize/update submodules: `git submodule update --recursive --init`
- Open the solution file `Embeddinator-4000.sln` with Visual Studio or Visual Studio For Mac
- Build

If you prefer to build from the command line Cake or Make can be used to build instead of Visual Studio For Mac. 

### Cake

The Android/C portions of the project can also be built with [Cake](https://cakebuild.net/) using the build.ps1 / build.sh scripts.

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

### Makefile

The Objective-C portions of the project can be built with `make` in `objcgen`.

### Nuget Generation

To generate the nuget one can use either (they both invoke the same build process):

- `make nuget` in `objcgen`
- [Cake](https://cakebuild.net/) :`./build.sh -t Create-Package`


## Usage

The getting started [documentation](https://docs.microsoft.com/en-us/xamarin/tools/dotnet-embedding/index) walks through basic usage of the Embeddinator. 

More details on platform specific invocations can be found [here](Usage.md).


## Development

The [contributing guide](Contributing.md) covers a number of areas to consider when contributing to Embeddinator-4000.

A number of internal documentation files exist describing the project and internal structure of Embeddinator:

- [General Project Structure](ProjectStructure.md)
- [Automated Tests](tests/Tests.md)
- [Objective-C Generator Internals](objcgen/Internals.md)

