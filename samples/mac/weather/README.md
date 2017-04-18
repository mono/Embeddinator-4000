# Weather Sample

This sample shows an Objective-C Mac app consuming a managed .NET assembly.

## Building instructions

You'll find two solutions

* **MyManagedStuff**: Managed C# library.
* **theweather** : An Objective-C Xcode project for a Mac App.

In order to run this sample, you need to run `make prepare` from the CLI, this will build the managed .NET library and use **Embeddinator-4000** on it to place the needed support files under `theweather/MyManagedStuff`. Once you have this files you can open `theweather` Xcode project and run it.