# MonoEmbeddinator-4000

![Embeddinator-4000 Logo](https://raw.githubusercontent.com/mono/mono-embeddinator4000/master/e4000-logo.png)

## Introduction

Embeddinator-4000 is a binding technology that acts as a bridge
between managed .NET libraries and other languages / platforms.

It is a tool that takes a .NET assembly and generates Mono
native C/C++ API bindings.

* Create a native API to access some C# APIs
* For now C bindings, in the future Obj-C binding, Java bindings

## Building

Open the solution in `build/MonoEmbeddinator4000.sln` with Visual Studio or Xamarin Studio and press F7.

## Usage

To generate bindings for a managed library you invoke the `MonoEmbeddinator4000.exe` command line tool.

If you do not pass any arguments, you will get a list of the tool options:

```
MonoEmbeddinator4000.exe [options]+ ManagedAssembly.dll
Generates target language bindings for interop with managed code.

      --gen=VALUE            target generator (C, C++, Obj-C)
  -o, --out=VALUE            output directory
  -c, --compile              compiles the generated output
      --dll, --shared        compiles as a shared library / DLL
  -v, --verbose              generates diagnostic verbose output
  -h, --help                 show this message and exit
```

To generate C bindings for a `Xamarin.Foo.dll` assembly you would call
the tool like:

`MonoEmbeddinator4000.exe -gen=c -out=foo Xamarin.Foo.dll`

Dependencies
------------

* IKVM.Reflection
* CppSharp

