MAKEFLAGS += --no-builtin-rules
.PHONY: java

all: binder managed gen compile_c_objc run 

binder:
	msbuild /nologo /v:minimal ../../build/MonoEmbeddinator4000.sln || xbuild /nologo /v:minimal ../../build/MonoEmbeddinator4000.sln

EMBEDDINATOR_EXE=../../build/lib/Debug/MonoEmbeddinator4000.exe
MANAGED_DLL=../managed/bin/Debug/managed.dll

gen:
	mono --debug $(EMBEDDINATOR_EXE) -gen=c -out=c -p=macos -compile -target=shared $(MANAGED_DLL)
	mono --debug $(EMBEDDINATOR_EXE) -gen=objc -out=objc -p=macos -compile -target=shared $(MANAGED_DLL)

BUILD_FLAGS=/v:minimal /p:DefineConstants="NON_OBJC_SUPPORTED_TESTS"

managed:
	BUILD_FLAGS="$(BUILD_FLAGS)" make -C ../managed clean all
	mkdir -p mk/bin/Debug
	cp ../managed/bin/Debug/managed.dll* mk/bin/Debug 

compile_c_objc:
	../../external/CppSharp/build/premake5-osx gmake
	make -C mk

JAVA_FILES=`find ../../support/java -name "*.java"` `find java -name "*.java"`
JUNIT_CLASSPATH=../../external/junit/hamcrest-core-1.3.jar:../../external/junit/junit-4.12.jar
JNA_CLASSPATH=../../external/jna/jna-4.3.0.jar

compile_java:
	mono --debug $(EMBEDDINATOR_EXE) -gen=java -out=java -p=macos -target=shared $(MANAGED_DLL)
	mkdir -p mk/java
	javac -cp $(JNA_CLASSPATH) -d mk/java $(JAVA_FILES)
	jar cf common.jar -C mk/java .
	javac -cp $(JNA_CLASSPATH):$(JUNIT_CLASSPATH):common.jar -Xlint:deprecation Tests.java

java: compile_java

run:
	mk/bin/Debug/common.Tests

clean:
	rm -rf mk c objc java *.dylib common.Tests