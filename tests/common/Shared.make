MAKEFLAGS += --no-builtin-rules

EMBEDDINATOR_EXE=../../build/lib/Debug/Embeddinator-4000.exe

MANAGED_DLL=../managed/generic/bin/Debug/managed.dll
MANAGED_PCL_DLL=../managed/pcl/bin/Debug/managed.dll
MANAGED_ANDROID_DLL=../managed/android/bin/Debug/managed.dll

BUILD_FLAGS=/v:minimal

ifeq ($(OS),Windows_NT)
# Defines
MSBUILD = msbuild.exe
PROJECT = ../../build/projects/Embeddinator-4000.csproj
EMBEDDINATOR_CMD= $(EMBEDDINATOR_EXE)
PLATFORM_FLAG= -p=windows
PATH_SEPERATOR=;
PREMAKE_GENERATE=../../external/CppSharp/build/premake5.exe --os=windows vs2015
BUILD_COMMON= msbuild.exe /nologo /v:minimal mk/mk.sln
else
# Defines
MSBUILD = /Library/Frameworks/Mono.framework/Versions/Current/Commands/msbuild
PROJECT = ../../build/projects/Embeddinator-4000.csproj
EMBEDDINATOR_CMD= mono --debug $(EMBEDDINATOR_EXE)
PLATFORM_FLAG= -p=macos
PATH_SEPERATOR=:
PREMAKE_GENERATE=../../external/CppSharp/build/premake5-osx gmake
BUILD_COMMON= make -C mk
endif
