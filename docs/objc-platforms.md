Objective-C Platforms
=====================

The embeddinator can target various platforms when generating Objective-C code:

* macOS
* iOS
* tvOS
* watchOS [not implemented yet]

The platform is selected by passing the `--platform=<platform>` command-line
argument to the embeddinator.

When building for the iOS, tvOS and watchOS platforms, the embeddinator will
always create a framework that embeds Xamarin.iOS, since Xamarin.iOS contains
a lot of runtime support code which is required on these platforms.

However, when building for the macOS platform, it's possible to choose whether
the generated framework should embed Xamarin.Mac or not. It's possible to not
embed Xamarin.Mac if the bound assembly does not reference Xamarin.Mac.dll
(either directly or indirectly), and this is selected by passing
`--platform=macOS` to the embeddinator.

If the bound assembly contains a reference to Xamarin.Mac.dll, it's necessary
to embed Xamarin.Mac, and additionally the embeddinator must know which target
framework to use.

There are three possible Xamarin.Mac target frameworks: `modern` (previously
called `mobile`), `full` and `system` (the difference between each is
described in Xamarin.Mac's [target framework][1] documentation), and each is
selected by passing `--platform=macOS-modern`, `--platform=macOS-full` or
`--platform=macOS-system` to the embeddinator.

[1]: https://developer.xamarin.com/guides/mac/advanced_topics/target-framework/
