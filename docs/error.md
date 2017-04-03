id:{932C3F0C-D968-42D1-BB14-D97C73361983}
title:Embeddinator-4000 errors

[//]: # (The original file resides under https://github.com/mono/Embeddinator-4000/tree/master/docs/error.md)
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

<h3><a name="EM0099"/>EM0099: Internal error *. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).</h3>

This error message is reported when an internal consistency check in the Embeddinator-4000 fails.

This indicates a bug in the Embeddinator-4000; please file a bug report at [https://github.com/mono/Embeddinator-4000/issues](https://github.com/mono/Embeddinator-4000/issues) with a test case.

# EM1xxx: code generation

<!-- 1xxx: code generation -->

<h3><a name="EM1001"/>EM1001: Can't generate binding code for a return value of type '*'.</h3>

The generator can't does not have knowledge about the type mentioned in the error message, and can't generate binding code for it.

Please file a bug report at [https://github.com/mono/Embeddinator-4000/issues](https://github.com/mono/Embeddinator-4000/issues) if this is a type the generator should be able to generate code for.

<h3><a name="EM1002"/>EM1002: Can't generate binding code for the type '*'.</h3>

The generator can't does not have knowledge about the type mentioned in the error message, and can't generate binding code for it.

Please file a bug report at [https://github.com/mono/Embeddinator-4000/issues](https://github.com/mono/Embeddinator-4000/issues) if this is a type the generator should be able to generate code for.

<!-- 2xxx: reserved -->
<!-- 3xxx: reserved -->
<!-- 4xxx: reserved -->
<!-- 5xxx: reserved -->
<!-- 6xxx: reserved -->
<!-- 7xxx: reserved -->
<!-- 8xxx: reserved -->
<!-- 9xxx: reserved -->
