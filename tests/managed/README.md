# managed test project

This directory serve as the base to create tests for generated code.

There is a shared project that contains all the source files, and then
multiple platform-specific projects in subfolders [1] that reference the
shared project.

The subdirectories are named according to the platform, so it should be
obvious what each project does (generic/managed-generic.csproj is a platform-
agnostic project).

Since all the code is included in all projects, platform-specific code must be
protected by conditional compilation to only compile on those platforms; all
other code must be pure .net code.

[1] In subfolders because msbuild does not like multiple projects in the same
directory; it's possible to work around it, but it's easier to just avoid the
problem.

