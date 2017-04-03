bin/Debug/managed.dll: $(wildcard *.cs) managed.csproj
	xbuild managed.csproj
	
all: managed.dll

clean:
	@rm -rf bin/ obj/