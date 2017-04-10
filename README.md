![Embeddinator-4000 Logo](e4000-logo.png)

| windows-vs-x86            | osx-clang-x86               |
|---------------------------|-----------------------------|
| [![windows-vs-x86][1]][2] | [![osx-clang-x86][3]][4]

[1]: https://ci.appveyor.com/api/projects/status/5o9gxjcttuaup671/branch/master?svg=true
[2]: https://ci.appveyor.com/project/mono/Embeddinator-4000/branch/master
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

This is a work in progress, the initial goals for this project are to
surface .NET to C, C++, Objective-C (across the various Apple platforms)
and Java (Android and regular Java).

The project is still under heavy development. Usage under mission-critical
production scenarios is not advised. Please report any bugs or suggestions
so we can improve the tool.

## Community

Feel free to join us at our [#managed-interop](https://gitter.im/managed-interop) Gitter discussion channel.

## Building

Clone this repository and initialize/update submodules as well as solution depends on them.

Open the solution in `build/MonoEmbeddinator4000.sln` with Visual Studio or Xamarin Studio and press F7.

## Usage

To generate bindings for a managed library you invoke the `MonoEmbeddinator4000.exe` command line tool.

_Important: please follow the instructions in `objcgen`'s [README](https://github.com/mono/Embeddinator-4000/blob/objc/objcgen/README.md) to use the new and improved Objective-C generator (will eventually fusion with `MonoEmbeddinator4000.exe`._

If you do not pass any arguments, you will get a list of the tool options:

```
MonoEmbeddinator4000.exe [options]+ ManagedAssembly.dll
Generates target language bindings for interop with managed code.

      --gen=VALUE            target language: c, c++, obj-c (defaults to c)
      --platform=VALUE       target platform: android, windows, macos, ios, watchos, tvos
  -o, --out=VALUE            output directory
  -c, --compile              compiles the generated output
      --dll, --shared        compiles as a shared library / DLL
      --vs=VALUE             Visual Studio version for compilation: 2012, 2013, 2015, Latest (defaults to Latest)
  -v, --verbose              generates diagnostic verbose output
  -h, --help                 show this message and exit
```

To generate C bindings for a `Xamarin.Foo.dll` assembly you would call
the tool like:

`MonoEmbeddinator4000.exe -gen=c -out=foo Xamarin.Foo.dll`

