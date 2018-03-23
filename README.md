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

Check out our [documentation to get started](https://mono.github.io/Embeddinator-4000/)

## Community

Feel free to join us at our [#managed-interop](https://gitter.im/managed-interop) Gitter discussion channel.

## Building

Clone this repository and initialize/update submodules as well as solution depends on them.

Open the solution file `Embeddinator-4000.sln` with Visual Studio or Xamarin Studio and press F7.

## Usage

To generate bindings for a managed library you invoke the `Embeddinator-4000.exe` command line tool.

_Important: please follow the instructions in `objcgen`'s [README](https://github.com/mono/Embeddinator-4000/blob/objc/objcgen/README.md) to use the new and improved Objective-C generator (will eventually fusion with `Embeddinator-4000.exe`)._

If you do not pass any arguments, you will get a list of the tool options:

```
Embeddinator-4000.exe [options]+ ManagedAssembly.dll
Generates target language bindings for interop with managed code.

      --gen=VALUE            target generator (C, C++, Obj-C, Java)
  -p, --platform=VALUE       target platform (iOS, macOS, Android, Windows)
  -e, --extension            compiles as an extension safe api
      --bitcode=VALUE        bitcode option (default, true, false)
  -o, --out, --outdir=VALUE  output directory
  -c, --compile              compiles the generated output
  -d, --debug                enables debug mode for generated native and
                               managed code
  -t, --target=VALUE         compilation target (static, shared, app)
      --dll, --shared        compiles as a shared library
      --static               compiles as a static library
      --vs=VALUE             Visual Studio version for compilation: 2012, 2013,
                               2015, 2017, Latest (defaults to Latest)
  -v, --verbose              generates diagnostic verbose output
  -h, --help                 show this message and exit
```

To generate C bindings for a `Xamarin.Foo.dll` assembly you would call
the tool like:

`Embeddinator-4000.exe -gen=c -out=foo Xamarin.Foo.dll`

