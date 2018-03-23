id:{932C3F0C-D968-42D1-BB14-D97C73361983}
title:Embeddinator-4000 errors

[//]: # (The original file resides under https://github.com/mono/Embeddinator-4000/tree/objc/docs/error.md)
[//]: # (This allows all contributors (including external) to submit, using a PR, updates to the documentation that match the tools changes)
[//]: # (Modifications outside of mono/Embeddinator-4000 will be lost on future updates)

# EM0xxx: binding error messages

E.g. parameters, environment

<!-- 0xxx: the generator itself, e.g. parameters, environment -->
<h3><a name="EM0000"/>EM0000: Unexpected error - Please fill a bug report at https://github.com/mono/Embeddinator-4000/issues</h3>

An unexpected error condition occurred. Please [file an issue](https://github.com/mono/Embeddinator-4000/issues) with as much information as possible, including:

* Full build logs, with maximum verbosity
* A minimal test case that reproduce the error
* All version informations

The easiest way to get exact version information is to use the **Xamarin Studio** menu, **About Xamarin Studio** item, **Show Details** button and copy/paste the version informations (you can use the **Copy Information** button).

<h3><a name="EM0001"/>EM0001: Could not create Output directory `X`</h3>

The directory name specified by `-o=DIR` does not exists and could not be created. It might be an invalid name for the file system.

<h3><a name="EM0002"/>EM0002: Option `X` is not supported</h3>

The tool does not support the option `X`. It is possible that another version of the tool supports it or that it does not apply in this environment.

<h3><a name="EM0003"/>EM0003: The platform `X` is not valid.</h3>

The tool does not support the platform `X`. It is possible that another version of the tool supports it or that it does not apply in this environment.

<h3><a name="EM0004"/>EM0004: The target `X` is not valid.</h3>

The tool does not support the target `X`. It is possible that another version of the tool supports it or that it does not apply in this environment.

<h3><a name="EM0005"/>EM0005: The compilation target `X` is not valid.</h3>

The tool does not support the compilation target `X`. It is possible that another version of the tool supports it or that it does not apply in this environment.

<h3><a name="EM0006"/>EM0006: Could not find the Xcode location.</h3>

The tool could not find the currently selected Xcode location using the `xcode-select -p` command. Please verify that this command succeeds, and returns the correct Xcode location.

<h3><a name="EM0007"/>EM0007: Could not get the sdk version for '{sdk}'.</h3>

The tool could not get the SDK version using the `xcrun --show-sdk-version --sdk {sdk}` command. Please verify that this command succeeds, and returns the SDK version.

<h3><a name="EM0008"/>EM0008: The architecture '{arch}' is not valid for {platform}. Valid architectures for {platform} are: '{architectures}'.</h3>

The architecture in the error message is not valid for the targeted platform. Please verify that the --abi option is passed a valid architecture.

<h3><a name="EM0009"/>EM0009: The feature `X` is not currently implemented by the generator</h3>

This is a known issue that we intend to fix in a future release of the generator. Contributions are welcome.

<h3><a name="EM0010"/>EM0010: Can't merge the frameworks '{simulatorFramework}' and '{deviceFramework}' because the file '{file}' exists in both.</h3>

The tool could not merge the frameworks mentioned in the error message, because there's a common file between them.

This might indicate a bug in the Embeddinator-4000; please file a bug report at [https://github.com/mono/Embeddinator-4000/issues](https://github.com/mono/Embeddinator-4000/issues) with a test case.

<h3><a name="EM0011"/>EM0011: The assembly `X` does not exist.</h3>

The tool could not find the assembly `X` specified in the arguments.

<h3><a name="EM0012"/>EM0012: The assembly name `X` is not unique</h3>

More than one assembly supplied have the same, internal name and it would not be possible to distinguish between them at runtime.

The most likely cause is that an assembly is specified more than once on the command-line arguments. However a renamed assembly still keeps it's original name and multiple copies cannot coexists.

<h3><a name="EM0013"/>EM0013: Can't find the assembly 'X', referenced by 'Y'.</h3>

The tool could not find the assembly 'X', referenced by the assembly 'Y'. Please make sure all referenced assemblies are in the same directory as the assembly to be bound.

<h3><a name="EM0014"/>EM0014: Could not find {product} ({product} {min_version} is required).</h3>

The dependency mentioned in the error message could not be found on the system.

<h3><a name="EM0015"/>EM0015: Could not find a valid version of {product} (found {version}, but at least {min_version} is required).</h3>

The dependency mentioned in the error message was found on the system, but it's too old. Please update to a newer version.

<h3><a name="EM0016">EM0016: Could not create symlink '{file}' -> '{target}': error {number}</h3>

Could not create the symlink mentioned in the error message.

<h3><a name="EM0017">EM0017: The bitcode option `X` is not valid.</h3>

The tool does not support the bitcode option `X`. Use either default, true or false.

<h3><a name="EM0026"/>EM0026 Could not parse the command line argument 'A': *</h3>

The syntax given for the command line option `A` could not be parsed by the tool. It is likely incorrect, please check with the documentation or help for the correct syntax.

<h3><a name="EM0099"/>EM0099: Internal error *. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).</h3>

This error message is reported when an internal consistency check in the Embeddinator-4000 fails.

This indicates a bug in the Embeddinator-4000; please file a bug report at [https://github.com/mono/Embeddinator-4000/issues](https://github.com/mono/Embeddinator-4000/issues) with a test case.


<!-- 1xxx: code processing -->

# EM1xxx: Code Processing

<h3><a name="EM1010"/>Type `T` is not generated because `X` are not supported.</h3>

This is a **warning** that the type `T` will be ignored (i.e. nothing will be generated) because it uses `X`, a feature that is not supported.

Note: Supported features will evolve with new versions of the tool.


<h3><a name="EM1011"/>Type `T` is not generated because it lacks a native counterpart.</h3>

This is a **warning** that the type `T` will be ignored (i.e. nothing will be generated) because it uses it expose something from the .NET framework that has no counterpart in the native platform.


<h3><a name="EM1011"/>Type `T` is not generated because it lacks marshaling code with a native counterpart.</h3>

This is a **warning** that the type `T` will be ignored (i.e. nothing will be generated) because it uses it expose something from the .NET framework that requires extra marshaling.

Note: This is something that is might get supported, with some limitations, in a future version of the tool.


<h3><a name="EM1020"/>Constructor `C` is not generated because of parameter type `T` is not supported.</h3>

This is a **warning** that the constructor `C` will be ignored (i.e. nothing will be generated) because a parameter of type `T` is not supported.

There should be an earlier warning giving more information why type `T` is not supported.

Note: Supported features will evolve with new versions of the tool.


<h3><a name="EM1021"/>Constructor `C` has default values for which no wrapper is generated.</h3>

This is a **warning** that the default parameters of constructor `C` are not generating any extra code. The most common cause is that an existing method already has the same signature. E.g. in .net it's possible to have:

```
public class MyType {
	public MyType () { ... }
	public MyType (int i = 0) { ... }
}
```

In such cases only two generated `init` selectors will be created, both calling into mono, but no wrapper for the later will exist.


<h3><a name="EM1030"/>Method `M` is not generated because return type `T` is not supported.</h3>

This is a **warning** that the method `M` will be ignored (i.e. nothing will be generated) because it's return type `T` is not supported.

There should be an earlier warning giving more information why type `T` is not supported.

Note: Supported features will evolve with new versions of the tool.


<h3><a name="EM1031"/>Method `M` is not generated because of parameter type `T` is not supported.</h3>

This is a **warning** that the method `M` will be ignored (i.e. nothing will be generated) because a parameter of type `T` is not supported.

There should be an earlier warning giving more information why type `T` is not supported.

Note: Supported features will evolve with new versions of the tool.


<h3><a name="EM1032"/>Method `M` has default values for which no wrapper is generated.</h3>

This is a **warning** that the default parameters of method `M` are not generating any extra code. The most common cause is that an existing method already has the same signature. E.g. in .net it's possible to have:

```
public class MyType {
	public int Increment () { ... }
	public int Increment (int i = 0) { ... }
}
```

In such cases only two generated `increment` selectors will be created, both calling into mono, but no wrapper for the later will exist.


<h3><a name="EM1033"/>Method `M` is not generated because another method exposes the operator with a friendly name.</h3>

This is a **warning** that the method `M` is not generated because another method exposes the operator with a friendly name. (https://msdn.microsoft.com/en-us/library/ms229032(v=vs.110).aspx)


<h3><a name="EM1034"/>Extension method `M` is not generated inside a category because they cannot be created on primitive type `T`. A normal, static method was generated.</h3>

This is a **warning** that an extension method on a primivite type (e.g. `System.Int32`) was found. In ObjC it is not possible to create categories on primitive type. Instead the generator will be produce a normal, static method.



<h3><a name="EM1040"/>Property `P` is not generated because of parameter type `T` is not supported.</h3>

This is a **warning** that the property `P` will be ignored (i.e. nothing will be generated) because the exposed type `T` is not supported.

There should be an earlier warning giving more information why type `T` is not supported.

Note: Supported features will evolve with new versions of the tool.

<h3><a name="EM1041"/>Indexed properties on `T` is not generated because multiple indexed properties are not supported.</h3>

This is a **warning** that the indexed properties on `T` will be ignored (i.e. nothing will be generated) because multiple indexed properties are not supported.


<h3><a name="EM1050"/>Field `F` is not generated because of field type `T` is not supported.</h3>

This is a **warning** that the field `F` will be ignored (i.e. nothing will be generated) because the exposed type `T` is not supported.

There should be an earlier warning giving more information why type `T` is not supported.

Note: Supported features will evolve with new versions of the tool.

<h3><a name="EM1051"/>Element `E` is generated instead as `F` because its name conflicts with an important objective-c selector.</h3>

This is a **warning** that the element `E` will be generated instead as `F` because its name conflicts with an important objective-c selector.

Selectors on the [NSObjectProtocol](https://developer.apple.com/reference/objectivec/1418956-nsobject?language=objc) have important meaning in objective-c and must be overridden carefully.

Note: The list of reserved selectors will evolve with new versions of the tool.


<h3><a name="EM1052"/>Element `E` is not generated its name conflicts with other elements on the same class.</h3>

This is a **warning** that Element `E` is not generated as its name conflicts with other elements on the same class.


<!-- 2xxx: code generation -->

# EM2xxx: Code Generation


<!-- 3xxx: reserved -->
<!-- 4xxx: reserved -->
<!-- 5xxx: reserved -->
<!-- 6xxx: reserved -->
<!-- 7xxx: reserved -->
<!-- 8xxx: reserved -->
<!-- 9xxx: reserved -->
