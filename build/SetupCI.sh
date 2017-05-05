BUILD_DIR=$(dirname -- $0)

if [ ! -f $BUILD_DIR/nuget.exe ]; then
	wget https://nuget.org/nuget.exe -O$BUILD_DIR/nuget.exe
fi

mono $BUILD_DIR/nuget.exe install Mono.TextTransform -OutputDirectory $BUILD_DIR/../deps
