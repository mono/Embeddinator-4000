### Tests

Given the task of processing arbitrary .NET assemblies and binding them to multiple languages, effective testing is crucial.

It is very easy for untested changes to regress previously working behavior. All new binding features should have both managed and unmanaged test to validate that they continue to work. 

In the event that binding features are landed without test coverage on all supported platforms, Github Issues should be opened to track the testing defect.

The "managed" tests test the core binding functionality are come in three parts:

- A managed-*platform* (or fsharp-*platform*) C# library that compiles the same files from the shared project into a C# library specific to that platform (macOS, iOS, Android, etc)
- A native (Objective-C, Java, C) application (objc-cli, android, common/Tests.C.cpp) which consumes the managed assembly after being bound and confirms expected behavior.
- A managed nunit test (objcgentest, MonoEmbeddinator4000.Tests) which invokes Embeddinator to bind the managed test library to the specific platform and then invokes the native application to test the bindings.
    - C is the exception here, which uses the "Run-C-Tests" target in  build/Tests.cake

- Beyond the "managed" tests, a few specialized test projects exist as well:
    - **MonoEmbeddinator4000.Tests** - Contains a number of Android specific tests as well
    - **leaktest** - A specialized leak checking test using leak-at-exit.c to hunt for unmanaged leaks from the mono runtime.
    - **managedwarn** - Along with objcgentest/ObjCGenErrWarnTests.cs used to test a number of tool warning scenarios.
