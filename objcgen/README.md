# ObjC generator

## Build

Simply run `make` in `Embeddinator-4000/objcgen`.

## Run

Don't use `Embeddinator-4000.exe`.

Use `mono ./bin/Debug/objcgen.exe --gen=Obj-C -o ./Output ManagedAssembly1.dll`.

## Generated API of questionable usability

Whenever we generate a working API that is not optimal (in ObjC) we should update the `docs/BestPracticesObjC.md` document to explain the situation and provide guidance to get the best ObjC API output.

## Missing .NET features

Unimplemented, but planned, features should throw a `NotImplementedException` with the feature name. The generator will report it as a missing feature that does not require a bug report (and test case).

## Unsupported .NET features

Anything that **cannot** be supported should be added to the `docs/Limitations.md` document.

The tool should issue warnings (which users can turn into errors) when it cannot generate ObjC to match any given .NET code. The warnings **must** be added to the `docs/errors.md` document.
