id:{8e48fb46-9a13-4a1b-a8af-9fe87458a293}
title:ObjC Support

[//]: # (The original file resides under https://github.com/mono/Embeddinator-4000/tree/objc/docs/ObjC.md)
[//]: # (This allows all contributors (including external) to submit, using a PR, updates to the documentation that match the tools changes)
[//]: # (Modifications outside of mono/Embeddinator-4000 will be lost on future updates)

# ObjC

## Specific features

The generation of ObjC has a few, special features that are worth noting.

### Automatic Reference Counting

The use of Automatic Reference Counting (ARC) is **required** to call the generated bindings. Project using a embeddinator-based library must be compiled with `-fobjc-arc`.

### NSString support

API that expose `System.String` types are converted into `NSString`. This makes memory management easier than dealing with `char*`.

### Protocols support

Managed interfaces are converted into ObjC protocols where all members are `@required`.

### NSObject Protocol support

By default we assume the default hashing and equality of both .net and the ObjC runtime are fine and interchangeable as they share very similar semantics.

When a managed type overrides `Equals(Object)` or `GetHashCode` then it generally means the defaut (.net) behaviour was not the best one. We can assume the default ObjC behaviour would not be either.

In such case the generator overrides the [`isEqual:`](https://developer.apple.com/reference/objectivec/1418956-nsobject/1418795-isequal?language=objc) method and [`hash`](https://developer.apple.com/reference/objectivec/1418956-nsobject/1418859-hash?language=objc) property defined in the [`NSObject` protocol](https://developer.apple.com/reference/objectivec/1418956-nsobject?language=objc). This allows the custom managed implementation to be used from ObjC code transparently.

### Comparison

Managed types that implement `IComparable` or it's generic version `IComparable<T>` will produce ObjC friendly methods that returns a `NSComparisonResult` and accept a `nil` argument. This makes the generated API more friendly to ObjC developers, e.g.

```
- (NSComparisonResult)compare:(XAMComparableType * _Nullable)other;
```


## Main differences with .NET

### Constructors v.s. Initializers

In Objective-C, you can call any of the initializer prototypes of any of the parent classes in the inheritance chain unless it is marked as unavailable (NS_UNAVAILABLE).

In C# you must explicitly declare a constructor member inside a class, this means constructors are not inherited.

In order to expose the right representation of the C# API to Objective-C, we add `NS_UNAVAILABLE` to any initializer that is not present in the child class from the parent class.

C# API:

```csharp
public class Unique {
	public Unique () : this (1)
	{
	}

	public Unique (int id)
	{
	}
}

public class SuperUnique : Unique {
	public SuperUnique () : base (911)
	{
	}
}
```

Objective-C surfaced API:

```objectivec
@interface SuperUnique : Unique

- (instancetype)initWithId:(int)id NS_UNAVAILABLE;
- (instancetype)init;

@end
```

Here we can see that `initWithId:` has been marked as unavailable.