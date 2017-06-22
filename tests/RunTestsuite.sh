OS=$(uname -s)
if [ "$OS" == "Darwin" ]; then
    export PATH=$PATH:/Library/Frameworks/Mono.framework/Versions/Current/bin
fi

CUR_DIR=$(dirname -- $0)
make -C $CUR_DIR/common
