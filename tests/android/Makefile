include ../common/Shared.make

compile:
	$(EMBEDDINATOR_CMD) -gen=java -out=mk -p=android -compile $(MANAGED_DLL)
	cp mk/managed.aar managed/managed.aar
	cp -R ../common/java/ app/src/main/java
	./gradlew assemble

install:
	./gradlew installDebug

run:
	./gradlew connectedAndroidTest

all: compile install run
