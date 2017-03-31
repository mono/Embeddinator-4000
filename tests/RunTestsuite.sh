msbuild /nologo /v:minimal ../build/MonoEmbeddinator4000.sln || xbuild /nologo /v:minimal ../build/MonoEmbeddinator4000.sln
make
./Basic/Basic.Tests