.PHONY: java

include Shared.make

all: binder managed c java run 

managed:
	BUILD_FLAGS="$(BUILD_FLAGS)" make -C ../managed clean all
	mkdir -p mk/bin/Debug
	cp $(MANAGED_DLL)* mk/bin/Debug 
	cp $(MANAGED_DLL)* .

# Targets
binder:
	$(MSBUILD) /nologo /v:minimal $(PROJECT)

c: gen_c compile_c_objc

gen_c:
	$(EMBEDDINATOR_CMD) -gen=c -out=c $(PLATFORM_FLAG) -compile -target=shared $(MANAGED_DLL)

gen_objc:
	$(EMBEDDINATOR_CMD) -gen=objc -out=objc -p=macos -compile -target=shared $(MANAGED_DLL)

compile_c_objc:
	$(PREMAKE_GENERATE)
	$(BUILD_COMMON)

JAVA_FILES=`find ../../support/java -name "*.java"` `find java -name "*.java"`
JUNIT_CLASSPATH=../../external/junit/hamcrest-core-1.3.jar:../../external/junit/junit-4.12.jar
JAVAC_FLAGS=-d mk/java -Xdiags:verbose -Xlint:deprecation

java: compile_java
	java -cp "$(subst :,$(PATH_SEPERATOR),java:mk/java:mk/java/managed.jar:$(JUNIT_CLASSPATH):.)" -D -Djna.dump_memory=true -Djna.library.path=c/ org.junit.runner.JUnitCore mono.embeddinator.Tests

compile_java:
	mkdir -p mk/java
	$(EMBEDDINATOR_CMD) -gen=java -out=mk/java $(PLATFORM_FLAG) -compile $(MANAGED_DLL)
	javac -cp "$(subst :,$(PATH_SEPERATOR),$(JUNIT_CLASSPATH):mk/java/managed.jar)" $(JAVAC_FLAGS) java/mono/embeddinator/Tests.java

android:
	make -C ../android compile deploy run

run:
	mk/bin/Debug/common.Tests

clean:
	rm -rf mk c objc *.dylib *.jar *.class *.dll
	make -C ../managed clean
