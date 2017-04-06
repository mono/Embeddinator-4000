# ObjC

## Specific features

The generation of ObjC has a few, special features that are worth noting.

### Automatic Reference Counting

The use of Automatic Reference Counting (ARC) is **required** to call the generated bindings. Project using a embeddinator-based library must be compiled with `-fobjc-arc`.

### NSString support

API that expose `System.String` types are converted into `NSString`. This makes memory management easier than dealing with `char*`.
