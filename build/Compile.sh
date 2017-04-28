#!/usr/bin/env bash
#set -e

CUR_DIR=$(cd "$(dirname "$0")"; pwd)

if [ "$(uname)" == "Darwin" ]; then
	PREMAKE=$CUR_DIR/../external/CppSharp/build/premake5-osx;
elif [ "$(expr substr $(uname -s) 1 5)" == "Linux" ]; then
	PREMAKE=$CUR_DIR/../external/CppSharp/build/premake5-linux-64;
fi

BUILD_CONF=release

$PREMAKE --file=$CUR_DIR/premake5.lua gmake
config=$BUILD_CONF make -C $CUR_DIR/gmake/

BUILD_CONF_DIR="$(tr '[:lower:]' '[:upper:]' <<< ${BUILD_CONF:0:1})${BUILD_CONF:1}"
BUILD_DIR=$CUR_DIR/gmake/lib/$BUILD_CONF_DIR
