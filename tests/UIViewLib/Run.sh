CUR_DIR=$(dirname -- $0)
EMBEDDINATOR=$CUR_DIR/../../build/lib/Debug/MonoEmbeddinator4000.exe

PLATFORM=macos
TARGET=shared
CONFIGURATION=Debug

NAME=UIViewLib
SOLUTION=$CUR_DIR/$NAME.sln
ASSEMBLY=$CUR_DIR/bin/$CONFIGURATION/$NAME.dll

while [[ "$#" > 1 ]]; do case $1 in
    --verbose) VERBOSE=true;;
    --v) verbose=true;;
    *) break;;
  esac; shift; shift
done

FLAGS=
VERBOSE=false

if [ "$VERBOSE" = true ]; then
	FLAGS+=-verbose
fi

msbuild /nologo /v:minimal $SOLUTION

for LANG in c objc java
do
	mono --debug $EMBEDDINATOR --gen=$LANG -compile -p=$PLATFORM -target=$TARGET $FLAGS --out=$CUR_DIR/gen/$LANG $ASSEMBLY
done
