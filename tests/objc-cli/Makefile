all: run-test perf xctest test-leaks test-static test-dynamic

OBJC_GEN_DIR=../../objcgen
OBJC_GEN=$(OBJC_GEN_DIR)/bin/Debug/objcgen.exe
$(OBJC_GEN): $(wildcard $(OBJC_GEN_DIR)/*.cs) $(wildcard ../../support/*)
	$(MAKE) -C $(OBJC_GEN_DIR)

MANAGED_DLL_DIR=../managed
MANAGED_DLL=$(MANAGED_DLL_DIR)/bin/Debug/managed.dll
$(MANAGED_DLL): $(wildcard $(MANAGED_DLL_DIR)/*.cs)
	make -C $(MANAGED_DLL_DIR)

managed.dll: $(MANAGED_DLL)
	cp $< $@

libmanaged.dylib: managed.dll $(OBJC_GEN)
	/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono --debug $(OBJC_GEN) --debug managed.dll -c

test-cli: test-managed.m libmanaged.dylib
	clang -g $< -lmanaged -L. -framework Foundation -o $@ -rpath @executable_path -fobjc-arc

perf-cli: perf-test.m libmanaged.dylib
	clang -g $< -lmanaged -L. -framework Foundation -o $@ -rpath @executable_path -fobjc-arc

run-test: test-cli
	./test-cli

perf: perf-cli
	time ./perf-cli

clean:
	@rm -f *.h glib.c mono_embeddinator.c bindings.m *.dylib *.dll test-cli perf-cli

xctest:
	xcodebuild test -project libmanaged/*.xcodeproj -scheme Tests

test-leaks: test-cli-leaks test-perf-leaks test-xctest-leaks

../leaktest/bin/Debug/leaktest.exe: $(wildcard ../leaktest/*.cs) libLeakCheckAtExit.dylib
	/Library/Frameworks/Mono.framework/Versions/Current/Commands/xbuild /verbosity:quiet /nologo ../leaktest/leaktest.csproj

test-cli-leaks: test-cli ../leaktest/bin/Debug/leaktest.exe
	mono --debug ../leaktest/bin/Debug/leaktest.exe $(abspath $<)

test-perf-leaks: perf-cli ../leaktest/bin/Debug/leaktest.exe
	mono --debug ../leaktest/bin/Debug/leaktest.exe $(abspath $<)

test-xctest-leaks: ../leaktest/bin/Debug/leaktest.exe $(MANAGED_DLL) libmanaged.dylib
	rm -Rf libmanaged/Tests.xctest
	xcodebuild -quiet build-for-testing -project libmanaged/*.xcodeproj -scheme Tests CONFIGURATION_BUILD_DIR=$(abspath libmanaged)
	mono --debug ../leaktest/bin/Debug/leaktest.exe `xcrun -f xctest` -XCTest All $(abspath libmanaged/Tests.xctest)

libLeakCheckAtExit.dylib: leak-at-exit.c
	clang -arch i386 -arch x86_64 -shared $< -o $@

test-static: test-static-macos test-static-ios test-static-tvos test-static-watchos

test-static-%: managed.dll $(OBJC_GEN)
	/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono --debug $(OBJC_GEN) --debug managed.dll -c --outdir=build/$@-temp-dir --target=staticlibrary --platform=$*

test-dynamic: test-dynamic-macos test-dynamic-ios test-dynamic-tvos test-dynamic-watchos

test-dynamic-%: managed.dll $(OBJC_GEN)
	/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono --debug $(OBJC_GEN) --debug managed.dll -c --outdir=build/$@-temp-dir --target=dylib --platform=$*
