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

	/usr/libexec/java_home -V

	# Specific dependencies for Travis-CI
	# See: https://docs.travis-ci.com/user/multi-os/
	if [ $TRAVIS_OS_NAME = 'osx' ]; then
		OPEN_JDK_URL=https://download.visualstudio.microsoft.com/download/pr/07d298ee-93e7-45cd-8d99-35d26e406508/280fb49bae77c2e4665af2b63d826be6
		OPEN_JDK_FILENAME=microsoft_dist_openjdk_1.8.0.25.zip
		JAVA8_DIR=/Library/Java/JavaVirtualMachines/jdk1.8.0_152.jdk/
		wget $OPEN_JDK_URL/$OPEN_JDK_FILENAME
		sudo unzip $OPEN_JDK_FILENAME -d $JAVA8_DIR
	fi

	/usr/libexec/java_home -V

elif [ "$OS" == "Linux" ]; then
	$BUILD_DIR/../external/CppSharp/build/InstallMono.sh
	sudo apt-get install fsharp
fi

cd $BUILD_DIR/..
