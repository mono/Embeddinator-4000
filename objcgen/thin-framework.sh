#!/bin/bash -ex

# This script will remove any simulator support from a framework, and any
# other files that are not needed in a final app.

# It will:
#   - Remove any simulator architectures from the executable
#   - Remove any managed libraries and related files that are simulator-specific
#   - Fix Info.plist to not say the framework is supporting the simulator anymore.
#   - Remove all headers (these are not needed once the app has been built).

IN_FRAMEWORK=
OUT_FRAMEWORK=

while ! test -z "$1"; do
	case "$1" in
		--input)
			IN_FRAMEWORK="$2"
			shift 2
			;;
		--output)
			OUT_FRAMEWORK="$2"
			shift 2
			;;
		*)
			echo "Unknown argument: $1"
			exit 1
			;;
	esac
done

if test -z "$IN_FRAMEWORK"; then
	echo "An input framework must be specified."
	exit 1
fi

if test -z "$OUT_FRAMEWORK"; then
	echo "An output framework must be specified."
	exit 1
fi

NAME=$(basename "$IN_FRAMEWORK")
NAME="${NAME%.*}"

LIPO_CMDS=
if xcrun lipo "$IN_FRAMEWORK/$NAME" -verify_arch i386; then
	LIPO_CMDS="$LIPO_CMDS -remove i386 "
fi
if xcrun lipo "$IN_FRAMEWORK/$NAME" -verify_arch x86_64; then
	LIPO_CMDS="$LIPO_CMDS -remove x86_64 "
fi

if test -z "$LIPO_CMDS"; then
	echo "The input framework was not built with simulator support"
	exit 1
fi


mkdir -p "$OUT_FRAMEWORK"
cp -R "$IN_FRAMEWORK/" "$OUT_FRAMEWORK/"
rm -Rf "$OUT_FRAMEWORK/MonoBundle/simulator"
rm -Rf "$OUT_FRAMEWORK/Headers"
xcrun lipo "$IN_FRAMEWORK/$NAME" "$LIPO_CMDS" -output "$OUT_FRAMEWORK/$NAME"

C=0
while PLATFORM=$(/usr/libexec/PlistBuddy -c "Print :CFBundleSupportedPlatforms:$C" "$OUT_FRAMEWORK/Info.plist" 2>/dev/null); do
	if [[ $PLATFORM == *Simulator* ]]; then
		/usr/libexec/PlistBuddy -c "Remove :CFBundleSupportedPlatforms:$C" "$OUT_FRAMEWORK/Info.plist"
	else
		(( C++ ))
	fi
done

echo "Framework $NAME thinned successfully into $OUT_FRAMEWORK."
