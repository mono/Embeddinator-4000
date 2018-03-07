include ../common/Shared.make

compile:
	$(EMBEDDINATOR_CMD) -gen=java -out=mk -p=android -compile $(MANAGED_ANDROID_DLL)
	cp mk/managed.aar managed/managed.aar
	cp -R ../common/java/ app/src/main/java
	./gradlew assemble

pcl-compile:
	$(EMBEDDINATOR_CMD) -gen=java -out=mk -p=android -compile $(MANAGED_PCL_DLL)
	cp mk/managed.aar managed/managed.aar
	cp -R ../common/java/ app/src/main/java
	./gradlew assemble

install:
	./gradlew installDebug

run:
	./gradlew connectedAndroidTest

pcl-run: pcl-compile install run

all: compile install run
