## Usage

Until tooling unification occurs, there are number of differences between invoking the Embeddinator between Objective-C and Java/C. 


### Objective-C 

To generate bindings to Objective-C for a managed library you invoke the `objcgen` command line tool.

If you do not pass any arguments, you will get a list of the tool options:

```
Generates target language bindings for interop with managed code.

Usage: objcgen [options]+ ManagedAssembly1.dll [ManagedAssembly2.dll ...]

  -c, --compile              Compiles the generated output
  -e, --extension            Compiles the generated output as extension safe api
      --nativeexception      Compiles the generated output to throw native
                               exceptions (Apple only)
  -d, --debug                Build the native library with debug information.
      --gen=VALUE            Target generator (default ObjectiveC)
      --abi=VALUE            A comma-separated list of ABIs to compile. If not
                               specified, all ABIs applicable to the selected
                               platform will be built. Valid values (also
                               depends on platform): i386, x86_64, armv7,
                               armv7s, armv7k, arm64.
  -o, --out, --outdir=VALUE  Output directory
  -p, --platform=VALUE       Target platform (iOS, macOS [default], macos-[
                               modern|full|system], watchOS, tvOS)
      --vs=VALUE             Visual Studio version for compilation (unsupported)
  -h, -?, --help             Displays the help
  -v, --verbose              generates diagnostic verbose output
      --version              Display the version information.
      --target=VALUE         The compilation target (staticlibrary,
                               sharedlibrary, framework).
      --warnaserror[=VALUE]  An optional comma-separated list of warning codes
                               that should be reported as errors (if no
                               warnings are specified all warnings are reported
                               as errors).
      --nowarn[=VALUE]       An optional comma-separated list of warning codes
                               to ignore (if no warnings are specified all
                               warnings are ignored).
```

To generate Objective-C bindings for a `Xamarin.Foo.dll` assembly you would call
the tool like:

`objcgen Xamarin.Foo.dll --target=framework --platform=macOS-modern --abi=x86_64 --outdir=output -c --debug`

with platform and abi depending on the specific target (macOS, iOS, etc).

### Java / C


To generate bindings to Java or C for a managed library you invoke the `Embeddinator-4000.exe` command line tool.

If you do not pass any arguments, you will get a list of the tool options:

```
Embeddinator-4000.exe [options]+ ManagedAssembly.dll
Generates target language bindings for interop with managed code.

      --gen=VALUE            target generator (C, C++, Obj-C, Java)
  -p, --platform=VALUE       target platform (iOS, macOS, Android, Windows)
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
