BUILD_DIR=$(dirname -- $0)

if [ ! -f $BUILD_DIR/nuget.exe ]; then
	wget https://nuget.org/nuget.exe -O$BUILD_DIR/nuget.exe
fi

mono $BUILD_DIR/nuget.exe install Mono.TextTransform -OutputDirectory $BUILD_DIR/../deps

TEXT_TEMPLATE_DIR="/Applications/Xamarin Studio.app/Contents/Resources/lib/monodevelop/AddIns/MonoDevelop.TextTemplating/"
mkdir -p "$TEXT_TEMPLATE_DIR"
cp -R $BUILD_DIR/../deps/Mono.TextTransform.1.0.0/tools "$TEXT_TEMPLATE_DIR"
