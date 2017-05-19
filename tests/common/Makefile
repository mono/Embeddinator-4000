MAKEFLAGS += --no-builtin-rules
.PHONY: java


EMBEDDINATOR_EXE=../../build/lib/Debug/MonoEmbeddinator4000.exe

MANAGED_DLL=../managed/generic/bin/Debug/managed.dll

BUILD_FLAGS=/v:minimal

all: binder managed c java run 

managed:
	BUILD_FLAGS="$(BUILD_FLAGS)" make -C ../managed clean all
	mkdir -p mk/bin/Debug
	cp $(MANAGED_DLL)* mk/bin/Debug 
	cp $(MANAGED_DLL)* .

ifeq ($(OS),Windows_NT)
# Defines
MSBUILD = msbuild.exe
PROJECT = ../../build/projects/MonoEmbeddinator4000.csproj
EMBEDDINATOR_CMD= $(EMBEDDINATOR_EXE)
PLATFORM_FLAG= -p=windows
PATH_SEPERATOR=;
PREMAKE_GENERATE=../../external/CppSharp/build/premake5.exe --os=windows vs2015
BUILD_COMMON= msbuild.exe /nologo /v:minimal mk/mk.sln
else
# Defines
MSBUILD = /Library/Frameworks/Mono.framework/Versions/Current/Commands/msbuild
PROJECT = ../../build/MonoEmbeddinator4000.sln
EMBEDDINATOR_CMD= mono --debug $(EMBEDDINATOR_EXE)
PLATFORM_FLAG= -p=macos
PATH_SEPERATOR=:
PREMAKE_GENERATE=../../external/CppSharp/build/premake5-osx gmake
BUILD_COMMON= make -C mk
endif

# Targets
binder:
	$(MSBUILD) /nologo /v:minimal $(PROJECT)

c: gen_c gen_cpp compile_c_objc_cpp

gen_c:
	$(EMBEDDINATOR_CMD) -gen=c -out=c $(PLATFORM_FLAG) -compile -target=shared $(MANAGED_DLL)

gen_cpp:
	$(EMBEDDINATOR_CMD) -gen=cpp -out=cpp $(PLATFORM_FLAG) -compile -target=shared $(MANAGED_DLL)

gen_objc:
	$(EMBEDDINATOR_CMD) -gen=objc -out=objc -p=macos -compile -target=shared $(MANAGED_DLL)

compile_c_objc_cpp:
	$(PREMAKE_GENERATE)
	$(BUILD_COMMON)

JAVA_FILES=`find ../../support/java -name "*.java"` `find java -name "*.java"`
JUNIT_CLASSPATH=../../external/junit/hamcrest-core-1.3.jar:../../external/junit/junit-4.12.jar
JNA_CLASSPATH=../../external/jna/jna-4.4.0.jar
JAVAC_FLAGS=-d mk/java -Xdiags:verbose -Xlint:deprecation

java: compile_java
	java -cp "$(subst :,$(PATH_SEPERATOR),mk/java:common.jar:$(JNA_CLASSPATH):$(JUNIT_CLASSPATH):.)" -D -Djna.dump_memory=true -Djna.library.path=c/ org.junit.runner.JUnitCore Tests

compile_java:
	$(EMBEDDINATOR_CMD) -gen=java -out=java $(PLATFORM_FLAG) -target=shared $(MANAGED_DLL)
	mkdir -p mk/java
	javac -cp "$(subst :,$(PATH_SEPERATOR),$(JNA_CLASSPATH):.)" $(JAVAC_FLAGS) $(JAVA_FILES)
	jar cf common.jar -C mk/java .
	javac -cp "$(subst :,$(PATH_SEPERATOR),$(JNA_CLASSPATH):$(JUNIT_CLASSPATH):common.jar)" $(JAVAC_FLAGS) Tests.java

run:
	mk/bin/Debug/common.Tests

clean:
	rm -rf mk c objc java *.dylib *.jar *.class *.dll
	make -C ../managed clean
