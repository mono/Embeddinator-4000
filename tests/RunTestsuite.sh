echo Disabled
exit 0
CUR_DIR=$(dirname -- $0)
msbuild /nologo /v:minimal $CUR_DIR/../build/MonoEmbeddinator4000.sln || xbuild /nologo /v:minimal $CUR_DIR/../build/MonoEmbeddinator4000.sln
make -C $CUR_DIR
$CUR_DIR/Basic/Basic.Tests