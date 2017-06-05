# objcgen warning and error tests

The objective is to test warning and error conditions that you normally can't using unit tests.

## Testing objcgen.exe

Currently, we have 2 test templates inside `Makefile` for testing *objcgen.exe* error and warning codes.

* **FindGenErrWarnCodeTemplate**: This is used to verify that error and warning codes are actually printed in *objcgen.exe* log output.
* **DoNotFindGenErrWarnCodeTemplate**: This is used to verify that error and warning are not printed in *objcgen.exe* log output.

In order to add additional tests create a new folder that includes the C# project with its files and add the following to the makefile:

```makefile
$(eval $(call {Template},{TEST},{csprojName},{Debug|Release},{[warn|error]code}))
```

For example:

```makefile
$(eval $(call FindGenErrWarnCodeTemplate,GenericsTest,GenericsLib,Debug,1010))
```

Also, do not forget to add your folder name into `TESTS` variable inside Makefile.

## Testing error conditions when using binding headers produced by objcgen.exe

Currently, we have 1 test template inside Makefile for testing binding headers.

* **FindXcodeErrWarnCodeTemplate**: This is used to verify that an error or a warning is printed in the log output when building an *xcodeproj*.

In order to add additional tests create a new folder that includes the C# project with its files that *objcgen.exe* will use as input, an Xcode project that uses the output headers of *objcgen.exe* and add the following to the makefile:

```makefile
$(eval $(call {Template},{TEST},{csprojName},{xcodeprojName},{xcodeTarget},{Debug|Release},{expected[warn|error]message}))
```

For example:

```makefile
$(eval $(call FindXcodeErrWarnCodeTemplate,NoInitInSubclassTest,ConstructorsLib,NoInitInSubclassTest,NoInitInSubclassTest,Release,"error: 'initWithId:' is unavailable"))
```

The `{expected[warn|error]message}` is given as a `grep` argument if it does not find it the test will fail. Also, do not forget to add your folder name into `TESTS` variable inside Makefile.

## Custom tests

If none of the above templates matches your testing needs feel free to add a makefile target with your own testing logic.
