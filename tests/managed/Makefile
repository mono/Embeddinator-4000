bin/Debug/managed.dll: properties.cs
	mkdir -p bin/Debug
	mcs $^ /out:$@ /target:library
	
all: managed.dll

clean:
	@rm -rf bin/ obj/