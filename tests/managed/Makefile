bin/Debug/managed.dll: $(wildcard *.cs) managed.csproj
	/Library/Frameworks/Mono.framework/Versions/Current/Commands/xbuild $(BUILD_FLAGS) managed.csproj
	
all: bin/Debug/managed.dll

clean:
	@rm -rf bin/ obj/