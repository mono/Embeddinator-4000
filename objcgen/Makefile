all: bin/Debug/objcgen.exe

OBJCGEN_FILES := \
	$(shell grep "Compile Include" *.csproj | grep -v Version.generated.cs | sed 's/.*Include="\(.*\)".*/\1/' | sed 's_\\_/_'g) \
	$(shell grep "EmbeddedResource Include" *.csproj | sed 's/.*Include="\(.*\)".*/\1/' | sed 's_\\_/_'g) \

bin/Debug/objcgen.exe: $(OBJCGEN_FILES)
	nuget restore ../generator.sln
	msbuild objcgen.csproj
