all: run-test perf

.PHONY: managed.dll

managed.dll:
	make -C ../managed
	cp ../managed/bin/Debug/managed.dll .

libmanaged.dylib: managed.dll
	xbuild ../../objcgen/objcgen.csproj
	mono ../../objcgen/bin/Debug/objcgen.exe managed.dll -c

test: libmanaged.dylib
	clang test-managed.m -lmanaged -L. -framework Foundation -o test-cli

run-test: test
	./test-cli
	
perf: libmanaged.dylib
	clang perf-test.m -lmanaged -L. -framework Foundation -o perf-cli
	time ./perf-cli

clean:
	@rm -f *.h *.c bindings.m *.dylib *.dll test-cli perf-cli
