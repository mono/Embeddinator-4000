all: run-test perf xctest test-leaks

.PHONY: managed.dll

managed.dll:
	make -C ../managed
	cp ../managed/bin/Debug/managed.dll .

libmanaged.dylib: managed.dll
	/Library/Frameworks/Mono.framework/Versions/Current/Commands/xbuild ../../objcgen/objcgen.csproj
	/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono --debug ../../objcgen/bin/Debug/objcgen.exe --debug managed.dll -c

test-cli: test-managed.m libmanaged.dylib
	clang -g $< -lmanaged -L. -framework Foundation -o $@ -rpath @executable_path

perf-cli: perf-test.m libmanaged.dylib
	clang -g $< -lmanaged -L. -framework Foundation -o $@ -rpath @executable_path

run-test: test-cli
	./test-cli

perf: perf-cli
	time ./perf-cli

clean:
	@rm -f *.h *.c bindings.m *.dylib *.dll test-cli perf-cli

xctest:
	xcodebuild test -project libmanaged/*.xcodeproj -scheme Tests

test-leaks: test-cli-leaks test-perf-leaks test-xctest-leaks

../leaktest/bin/Debug/leaktest.exe: $(wildcard ../leaktest/*.cs) libLeakCheckAtExit.dylib
	/Library/Frameworks/Mono.framework/Versions/Current/Commands/xbuild ../leaktest/leaktest.csproj

test-cli-leaks: test-cli ../leaktest/bin/Debug/leaktest.exe
	whoami
	groups
	/usr/sbin/DevToolsSecurity -status
	mono --debug ../leaktest/bin/Debug/leaktest.exe $(abspath $<)

test-perf-leaks: perf-cli ../leaktest/bin/Debug/leaktest.exe
	mono --debug ../leaktest/bin/Debug/leaktest.exe $(abspath $<)

test-xctest-leaks:
	rm -Rf libmanaged/Tests.xctest
	xcodebuild  build-for-testing -project libmanaged/*.xcodeproj -scheme Tests CONFIGURATION_BUILD_DIR=$(abspath libmanaged)
	mono --debug ../leaktest/bin/Debug/leaktest.exe `xcrun -f xctest` -XCTest All $(abspath libmanaged/Tests.xctest)

libLeakCheckAtExit.dylib: leak-at-exit.c
	clang -arch i386 -arch x86_64 -shared $< -o $@
