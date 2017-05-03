# UnitsNet 

This is a sample to bind an existing .net library, UnitsNet, as a native library usable from ObjC.

github: https://github.com/anjdreas/UnitsNet
nuget: https://www.nuget.org/packages/UnitsNet/

Right now there are **many** warnings for API that cannot be generated. Most of the warnings comes from the use of:

* `System.Nullable<T>`, see issue [#229](https://github.com/mono/Embeddinator-4000/issues/229)
* `System.Decimal`, see issue [#254](https://github.com/mono/Embeddinator-4000/issues/254)
* `System.TimeSpan`, discussed in issue [#176](https://github.com/mono/Embeddinator-4000/issues/176)
* `System.IFormatProvider`, see issue [#175](https://github.com/mono/Embeddinator-4000/issues/175)

Still the embeddinator is able to generate a fat (i386/x86_64) `libUnitsNet.dylib` usable with an ObjC-written macOS application.
