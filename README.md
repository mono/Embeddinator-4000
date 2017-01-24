![Embeddinator-4000 Logo](e4000-logo.png)

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

## Building

Open the solution in `build/MonoEmbeddinator4000.sln` with Visual Studio or Xamarin Studio and press F7.

## Usage

To generate bindings for a managed library you invoke the `MonoEmbeddinator4000.exe` command line tool.

If you do not pass any arguments, you will get a list of the tool options:

```
MonoEmbeddinator4000.exe [options]+ ManagedAssembly.dll
Generates target language bindings for interop with managed code.

      --gen=VALUE            target language: c, c++, obj-c (defaults to c)
      --platform=VALUE       target platform: android, windows, macos, ios, watchos, tvos
  -o, --out=VALUE            output directory
  -c, --compile              compiles the generated output
      --dll, --shared        compiles as a shared library / DLL
  -v, --verbose              generates diagnostic verbose output
  -h, --help                 show this message and exit
```

To generate C bindings for a `Xamarin.Foo.dll` assembly you would call
the tool like:

`MonoEmbeddinator4000.exe -gen=c -out=foo Xamarin.Foo.dll`

