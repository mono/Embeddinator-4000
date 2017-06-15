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

APIs that expose `System.String` types are converted into `NSString`. This makes memory management easier than dealing with `char*`.

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

### Categories

Managed extensions methods are converted into categories. For example the following extension methods on `Collection`

```
	public static class SomeExtensions {

		public static int CountNonNull (this Collection collection) { ... }

		public static int CountNull (this Collection collection) { ... }
	}
```

would create an ObjC category like this one:

```
@interface Collection (SomeExtensions)

- (int)countNonNull;
- (int)countNull;
@end
```

When a single managed type extends several types then multiple ObjC categories are generated.

### Subscripting

Managed indexed properties are converted into object subscripting. For example:

```
	public bool this[int index] {
		get { return c[index]; }
		set { c[index] = value; }
	}
```

would create ObjC similar to :

```
- (id)objectAtIndexedSubscript:(int)idx;
- (void)setObject:(id)obj atIndexedSubscript:(int)idx;
```

which can be used via the ObjC subscripting syntax:

```
    if ([intCollection [0] isEqual:@42])
	    intCollection[0] = @13;
```

Depending on the type of your indexer, indexed or keyed subscripting will be generated where appropriate. 

This [article](http://nshipster.com/object-subscripting/) is a great introduction to subscripting.

## Main differences with .NET

### Constructors vs Initializers

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

### Operator

ObjC does not support operator overloading as C# does, so operators are converted to class selectors:

```
	public static AllOperators operator + (AllOperators c1, AllOperators c2)
	{
		return new AllOperators (c1.Value + c2.Value);
	}
```

to

```
+ (instancetype)add:(Overloads_AllOperators *)anObjectC1 c2:(Overloads_AllOperators *)anObjectC2;
```

However, some .NET languages do not support operator overloading, so it is common to also include a ["friendly"](https://msdn.microsoft.com/en-us/library/ms229032(v=vs.110).aspx) named method in addition to the operator overload.

If both the operator version and the "friendly" version are found, only the friendly version will be generated, as they will generate to the same objective-c name.

```
	public static AllOperatorsWithFriendly operator + (AllOperatorsWithFriendly c1, AllOperatorsWithFriendly c2)
	{
		return new AllOperatorsWithFriendly (c1.Value + c2.Value);
	}

	public static AllOperatorsWithFriendly Add (AllOperatorsWithFriendly c1, AllOperatorsWithFriendly c2)
	{
		return new AllOperatorsWithFriendly (c1.Value + c2.Value);
	}
```

becomes:

```
+ (instancetype)add:(Overloads_AllOperatorsWithFriendly *)anObjectC1 c2:(Overloads_AllOperatorsWithFriendly *)anObjectC2;
```

### Equality operator

In general operator == in C# is handled as a general operator as noted above.

However, if the "friendly" Equals operator is found, both operator == and operator != will be skipped in generation.

### DateTime vs NSDate

From [NSDate's](https://developer.apple.com/reference/foundation/nsdate?language=objc) documentation:

> NSDate objects encapsulate a single point in time, independent of any particular calendrical system or time zone. Date objects are immutable, representing an invariant time interval relative to an absolute reference date (00:00:00 UTC on 1 January 2001).

Due to `NSDate` reference date, all conversions between it and `DateTime` must be done in UTC.

#### DateTime to NSDate

When converting from `DateTime` to `NSDate` the DateTime's `Kind` property is taken into account.

| Kind         | Results                                                                                            |
| ------------ | -------------------------------------------------------------------------------------------------- |
| Utc          | Conversion is performed using the provided DateTime object as is.                                  |
| Local        | The result of calling `ToUniversalTime ()` in the provided DateTime object is used for conversion. |
| Unspecified  | The provided DateTime object is assumed to be UTC, so same behavior as Kind == Utc.                |

The conversion is done by using the following formula:

**TimeInterval** = DateTimeObjectTicks - NSDateReferenceDateTicks[dt] / [TicksPerSecond](https://msdn.microsoft.com/en-us/library/system.timespan.tickspersecond(v=vs.110).aspx)

Once we have the TimeInterval we use NSDate's [dateWithTimeIntervalSinceReferenceDate:](https://developer.apple.com/reference/foundation/nsdate/1591577-datewithtimeintervalsincereferen?language=objc) selector to create it.

#### NSDate to DateTime

Going from NSDate to DateTime we assume we are getting a NSDate instance which's reference date is **00:00:00 UTC on 1 January 2001** and use the following formula:

**DateTimeTicks** = NSDateTimeIntervalSinceReferenceDate * [TicksPerSecond](https://msdn.microsoft.com/en-us/library/system.timespan.tickspersecond(v=vs.110).aspx) + NSDateReferenceDateTicks[dt]

Once we calculated the **DateTimeTicks** we use the following DateTime [constructor](https://msdn.microsoft.com/en-us/library/w0d47c9c(v=vs.110).aspx) setting its `kind` to `DateTimeKind.Utc`.

There are some considerations that you must be aware of, NSDate can be `nil` but a DateTime is a struct in .NET and by definition it can't be `null`. If you give a `nil` NSDate we will translate it to the default DateTime value which maps to `DateTime.MinValue`.

MinValue and MaxValue are also different, NSDate can support greater and lower boundaries than DateTime's so whenever you give a greater or lower value we will set it to DateTime's [MaxValue](https://msdn.microsoft.com/en-us/library/system.datetime.maxvalue(v=vs.110).aspx) or [MinValue](https://msdn.microsoft.com/en-us/library/system.datetime.minvalue(v=vs.110).aspx) respectively.

**dt**: NSDate reference date **00:00:00 UTC on 1 January 2001** => `new DateTime (year:2001, month:1, day:1, hour:0, minute:0, second:0, kind:DateTimeKind.Utc).Ticks;`