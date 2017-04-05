bin/Debug/managed.dll: $(wildcard *.cs) managed.csproj
	/Library/Frameworks/Mono.framework/Versions/Current/Commands/xbuild managed.csproj
	
all: managed.dll

clean:
	@rm -rf bin/ obj/