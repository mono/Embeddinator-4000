# ObjC

## Specific features

The generation of ObjC has a few, special features that are worth noting.

### Automatic Reference Counting

The use of Automatic Reference Counting (ARC) is **required** to call the generated bindings. Project using a embeddinator-based library must be compiled with `-fobjc-arc`.

### NSString support

API that expose `System.String` types are converted into `NSString`. This makes memory management easier than dealing with `char*`.

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