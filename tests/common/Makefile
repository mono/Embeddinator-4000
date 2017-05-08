MAKEFLAGS += --no-builtin-rules
.PHONY: java

all: binder managed c java run 

binder:
	msbuild /nologo /v:minimal ../../build/MonoEmbeddinator4000.sln || xbuild /nologo /v:minimal ../../build/MonoEmbeddinator4000.sln

EMBEDDINATOR_EXE=../../build/lib/Debug/MonoEmbeddinator4000.exe
MANAGED_DLL=../managed/generic/bin/Debug/managed.dll

gen_c:
	mono --debug $(EMBEDDINATOR_EXE) -gen=c -out=c -p=macos -compile -target=shared $(MANAGED_DLL)

gen_objc:
	mono --debug $(EMBEDDINATOR_EXE) -gen=objc -out=objc -p=macos -compile -target=shared $(MANAGED_DLL)

BUILD_FLAGS=/v:minimal

managed:
	BUILD_FLAGS="$(BUILD_FLAGS)" make -C ../managed clean all
	mkdir -p mk/bin/Debug
	cp $(MANAGED_DLL)* mk/bin/Debug 
	cp $(MANAGED_DLL)* .

compile_c_objc:
	../../external/CppSharp/build/premake5-osx gmake
	make -C mk

c: gen_c compile_c_objc

JAVA_FILES=`find ../../support/java -name "*.java"` `find java -name "*.java"`
JUNIT_CLASSPATH=../../external/junit/hamcrest-core-1.3.jar:../../external/junit/junit-4.12.jar
JNA_CLASSPATH=../../external/jna/jna-4.3.0.jar
JAVAC_FLAGS=-d mk/java -Xdiags:verbose -Xlint:deprecation

compile_java:
	mono --debug $(EMBEDDINATOR_EXE) -gen=java -out=java -p=macos -target=shared $(MANAGED_DLL)
	mkdir -p mk/java
	javac -cp $(JNA_CLASSPATH) -d mk/java $(JAVAC_FLAGS) $(JAVA_FILES)
	jar cf common.jar -C mk/java .
	javac -cp $(JNA_CLASSPATH):$(JUNIT_CLASSPATH):common.jar $(JAVAC_FLAGS) Tests.java

java: compile_java
	cp c/libmanaged.dylib .
	java -cp mk/java:common.jar:$(JNA_CLASSPATH):$(JUNIT_CLASSPATH) org.junit.runner.JUnitCore Tests

run:
	mk/bin/Debug/common.Tests

clean:
	rm -rf mk c objc java *.dylib *.jar *.class *.dll
	make -C ../managed clean
