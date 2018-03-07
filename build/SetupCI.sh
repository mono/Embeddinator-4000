if [ -n "$TRAVIS_BUILD_DIR" ]; then
BUILD_DIR=$TRAVIS_BUILD_DIR/build
fi;

if [ -n "$BUILD_SOURCESDIRECTORY" ]; then
BUILD_DIR=$BUILD_SOURCESDIRECTORY/build
fi

OS=$(uname -s)
if [ "$OS" == "Darwin" ]; then
	cd $BUILD_DIR/../objcgen
	./system-dependencies.sh --provision

	export PATH=$PATH:/Library/Frameworks/Mono.framework/Versions/Current/bin
elif [ "$OS" == "Linux" ]; then
	$BUILD_DIR/../external/CppSharp/build/InstallMono.sh
	sudo apt-get install fsharp
fi

cd $BUILD_DIR/..
