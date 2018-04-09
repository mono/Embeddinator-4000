## Objective-C Generator Internals

Code generators by their nature of generating code that will later be compiled can be difficult to trace through and understand. 

The Objective-C backend covers a number of different platforms (macOS, iOS, tvOS, etc). For all platforms, other than macOS without Xamarin.Mac, building frameworks is the default and supported packaging technique. Static libraries and dylib are available on macOS without Xamarin.Mac.

This document will provide a high level roadmap of the components of `objcgen` and how they fit together.

### Flow of Execution

- Executation begins in the [driver](driver.cs) which handles a few tasks:
    - 	Use [Mono.Options](https://github.com/xamarin/XamarinComponents/tree/master/XPlat/Mono.Options) to parse command line arguments
    -  Setup error handling in case of later crashes/exceptions
    -  In the common "generate" action case, instance an [embedder](embedder.cs) and configure it based on command line arguments
    -  If compilation is requested then the [driver](driver.cs) invokes `Compile ()` on the [embedder](embedder.cs) post generation 
-  The [embedder](embedder.cs) drives code generation and packaging by:
    -  Validating settings passed in by [driver](driver.cs) are valid based on platform specific rules
    -  Use [IKVM](https://github.com/mono/ikvm-fork) to load the .NET assembly in question for processing via reflection.
        -  Also configure IKVM to look for BCL and Facades from the appropriate SDK directory
    -  Instance a [ObjCProcessor](objcprocessor.cs) which reflects the library extracting needed information.
    -  Instance a [ObjCGenerator](objcgenerator.cs) which uses the ProcessedAssembly data to generate the native Objective-C bindings.
-  The [ObjCProcessor](objcprocessor.cs) walks each "acceptable" type creating various ["Processed"](processedtypes.cs) data structures for each Type, Method, Property, etc.
   -  Acceptable types a subset of public, non-NSObject subclasses that we support.
   -  Some type references (such as DateType) require binding additional BCL types to be useable in Objective-C and are pulled in "magically" during processing.
   -  This is the first, and best, stage where items can be removed from binding if they require features not yet supported.
   -  A number of mappings are done from C# to Objective-C to make APIs more friendly, such as nicely exposing subscripting.
   -  Items are all OrderBy'ed to force generation to produce consisently ordered output later.
   -  As each catagory of items is handled (types, methods, etc) a [postprocessor](objcgenerator-postprocessor.cs) walks the data.
-  This [postprocessor](objcgenerator-postprocessor.cs) analyzes each catagory looking for items that will cause trouble later in generation\compilation or produce suboptimal bindings:
	- Names that will produce identical selectors or shadow important pre-existing Objective-C selectors
		- Duplication detection is done via the [Type Mapper](TypeMapper.cs).
	- [Operator Overloads](OperatorOverloads.cs) can often be exposed in more friendly names than `op_Addition` and are renamed where possible. Where both "friendly" named and operator methods exist, we expose only one copy.
	- Each ["processed"](processedtypes.cs) data structure is then "frozen" so that we can cache generated data (such as names) only after no additional changes will occur. Processed types should now be considered effectively immutable.
- Now that we have a hierarchy of  ["processed"](processedtypes.cs) data types, we can finally enter the [ObjCGenerator](objcgenerator.cs).
	- [SourceWriters](sourcewriter.cs) are created for headers/private headers/implementations to buffer text until it is written to disk and readably handle indentation.
	- After writing the standard introduction parts to each file, each assembly is processed in turn.
	- Each Enum/Protocol/Type/Extension from that assembly is then generated in turn, each from a GenerateFoo method. 
		- Some but not all Generation methods depend on "helpers" such as [ProtocolHelper](protocolhelper.cs) which help generate correct code.
		- [NameGenerator](NameGenerator.cs) contains the mapping between C# and Objective-C names for types/arguments.
- If compilation is requested then the [Embedder's](embedder.cs) Compile () generates and executes clang invocations.
    - Special post processing occurs in some target types, frameworks for example, and may involve moving files / lipo / etc
