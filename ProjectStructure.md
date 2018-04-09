## Project Structure

The Embeddinator-4000 project covers a number of languages (Objective-C, Java, C) and is made up of a number of different components.

The most major current distinction is between the Android/C generators and Objective-C generator. Currently these areas are quite distinct, with different folders and projects, without a lot of overlap. Future work is planned to harmonize the differences where possible.

### Projects

- Embeddinator-4000.sln is the top solution which contains all Embeddinator projects. 
- build/projects/Embeddinator-4000.csproj contains the Java and C backends and is interally called "binder" in a number of build files.
- objcgen/objcgen.csproj contains the Objective-C backend and is exposed as the objcgen tool.

### Folders

Here is a high level description of what each folder contains:

- **binder** - Android/C backend code
- **build** - Cake build files and projects used primariy for Android/C.
    - nuget generation is here as well, and shared by all platforms.
- **docs** - **Temporary** - External user facing documentation. This will move to docs.microsoft.com soon.
- **external** - External libraries used, pulled in as submodules, which include:
   - IKVM - Used by Objective-C backend
   - CppSharp - Used by Android/C backend 
- **objcgen** - Makefiles and code for the Objective-C backend
- **packages** - External libraries used, pulled in as nugets, which include:
   - Mono.Cecil - Used by Java/C backend
   - NUnit - Powering unit tests
   - A large number of Xamarin.Android support libraries used by Java backend, primarily in tests.
- **samples** - A handful of samples libraries showing use of Embeddinator
- **support** - Native (Objective-C, Java, and C) headers and files consumed by the generated bindings
- **tests** - Automated test suite described in more detail [here](tests/Tests.md) here.
- **tools** - Developer scripts and internal tooling
