all: run-test perf xctest

.PHONY: managed.dll

managed.dll:
	make -C ../managed
	cp ../managed/bin/Debug/managed.dll .

libmanaged.dylib: managed.dll
	/Library/Frameworks/Mono.framework/Versions/Current/Commands/xbuild ../../objcgen/objcgen.csproj
	/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono --debug ../../objcgen/bin/Debug/objcgen.exe --debug managed.dll -c

test: libmanaged.dylib
	clang test-managed.m -lmanaged -L. -framework Foundation -o test-cli -rpath @executable_path -fobjc-arc

run-test: test
	./test-cli
	
perf: libmanaged.dylib
	clang perf-test.m -lmanaged -L. -framework Foundation -o perf-cli -rpath @executable_path -fobjc-arc
	time ./perf-cli

clean:
	@rm -f *.h *.c bindings.m *.dylib *.dll test-cli perf-cli

xctest:
	xcodebuild test -project libmanaged/*.xcodeproj -scheme Tests
