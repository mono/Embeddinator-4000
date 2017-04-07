.PHONY: all binder managed gen compile run clean
MAKEFLAGS += --no-builtin-rules

all: binder managed gen compile run 

binder:
	msbuild /nologo /v:minimal ../../build/MonoEmbeddinator4000.sln || xbuild /nologo /v:minimal ../../build/MonoEmbeddinator4000.sln

EMBEDDINATOR_EXE=../../build/lib/Debug/MonoEmbeddinator4000.exe
MANAGED_DLL=../managed/bin/Debug/managed.dll

gen:
	mono --debug $(EMBEDDINATOR_EXE) -gen=c -out=c -p=macos -compile -target=shared $(MANAGED_DLL)
	mono --debug $(EMBEDDINATOR_EXE) -gen=objc -out=objc -p=macos -compile -target=shared $(MANAGED_DLL)
	mono --debug $(EMBEDDINATOR_EXE) -gen=java -out=java -p=macos -target=shared $(MANAGED_DLL)

BUILD_FLAGS=/v:minimal /p:DefineConstants="NON_OBJC_SUPPORTED_TESTS"

managed:
	BUILD_FLAGS="$(BUILD_FLAGS)" make -C ../managed clean all
	mkdir -p mk/bin/Debug
	cp ../managed/bin/Debug/managed.dll* mk/bin/Debug 

compile:
	../../external/CppSharp/build/premake5-osx gmake
	make -C mk

run:
	mk/bin/Debug/Basic.Tests

clean:
	rm -rf mk *.dylib Basic.Tests