all: run-test perf xctest

.PHONY: managed.dll

managed.dll:
	make -C ../managed
	cp ../managed/bin/Debug/managed.dll .

libmanaged.dylib: managed.dll
	/Library/Frameworks/Mono.framework/Versions/Current/Commands/xbuild ../../objcgen/objcgen.csproj
	/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono --debug ../../objcgen/bin/Debug/objcgen.exe --debug managed.dll -c

test-cli: test-managed.m libmanaged.dylib
	clang -g $< leak-check.c -lmanaged -L. -framework Foundation -o $@ -rpath @executable_path -fobjc-arc

perf-cli: perf-test.m libmanaged.dylib
	clang -g $< leak-check.c -lmanaged -L. -framework Foundation -o $@ -rpath @executable_path -fobj-arc

run-test: test-cli
	./test-cli

perf: perf-cli
	time ./perf-cli

clean:
	@rm -f *.h *.c bindings.m *.dylib *.dll test-cli perf-cli

xctest:
	xcodebuild test -project libmanaged/*.xcodeproj -scheme Tests
