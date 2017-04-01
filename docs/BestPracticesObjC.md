id:{932C3F0C-D968-42D1-BB14-D97C73361983}
title:Embeddinator-4000 Best Practices for ObjC

[//]: # (The original file resides under https://github.com/mono/Embeddinator-4000/tree/master/docs/BestPracticesObjC.md)
[//]: # (This allows all contributors (including external) to submit, using a PR, updates to the documentation that match the tools changes)
[//]: # (Modifications outside of mono/Embeddinator-4000 will be lost on future updates)

This is a draft and might not be in-sync with the features presently supported by the tool. We hope that this document will evolve separately and eventually match the final tool, i.e. we'll suggest the long term best approaches - not immediate workarounds.

A large part of the advices from this document also applies to any other supported languages. However all the provided examples are in C# and ObjC.


# Exposing a subset of the managed code

The generated native library/framework contains ObjC code to call each of the managed API that is exposed. The more API you surface (public) then larger the library will become.

It might be a good idea to create a different, smaller assembly, to expose only the required API to the native developer. That facade will also allow you more control over the visibility, naming, error checking... of the generated code.

E.g.

```
namespace Xamarin.Xml.Configuration {
	public class Reader {}
}
```

might be exposed as

```
public class XAMXmlConfigReader : Xamarin.Xml.Configuration.Reader {}
```

making it more ObjC friendly to use.

```
id reader = [[XAMXmlConfigReader alloc] init];
```

versus

```
id reader = [[Xamarin_Xml_Configuration_.Reader alloc] init];
```


# Exposing a chunkier API

There is a price to pay to transition from native to managed (and back). As such your better to expose a _chunky instead of chatty_ API to the native developers, e.g.

**Chatty**
```
public class Person {
	public string FirstName { get; set; }
	public string LastName { get; set; }
}
```

```
// this requires 3 calls / transitions to initialize the instance
Person *p = [[Person alloc] init];
p.firstName = @"Sebastien";
p.LastName = @"Pouliot";
```

**Chunky**
```
public class Person {
	public Person (string firstName, string lastName) {}
}
```

```
// a single call / transition will perform better
Person *p = [[Person alloc] initWithFirstName:@"Sebastien" LastName:"Pouliot"];
```

Since the number of transition being smaller the performance will be better. It also requires less code to be generated so it will produce a smaller native library as well.


# Naming

A good .NET name might not be ideal for an ObjC API. From an ObjC developer point of view a method with a `Get` prefix implies you do not own the instance, i.e. the [get rule](https://developer.apple.com/library/content/documentation/CoreFoundation/Conceptual/CFMemoryMgmt/Concepts/Ownership.html#//apple_ref/doc/uid/20001148-SW1).

However a .NET method with a `Create` prefix will behave identically - but for ObjC developers it normally means you own the returned instance, i.e. the [create rule](https://developer.apple.com/library/content/documentation/CoreFoundation/Conceptual/CFMemoryMgmt/Concepts/Ownership.html#//apple_ref/doc/uid/20001148-103029).


# Exceptions

It's quite commont in .NET to use exceptions extensively to report errors. However they are slow and not quite identical in ObjC. Whenever possible you should hide them from the ObjC developer, e.g.

For example .NET `Try` pattern will be much easier to consume from ObjC code.

```
public int Parse (string number)
{
	return Int32.Parse (number);
}
```

versus

```
public bool TryParse (string number, out int value)
{
	return Int32.TryParse (number, out value);
}
```
