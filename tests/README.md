Test suite
----------

This directory contains the test suite for Embeddinator.

To run the test suite, run `RunTestsuite.sh` inside this directory.

The test suite build files are automatically generated from Premake build scripts.
This makes sure the test suite can be run as part of an MSBuild-based system (VS on Windows)
as well as a POSIX-based Make system.

To re-generate the test suite, run:

```
../external/CppSharp/build/premake5-osx gmake
```