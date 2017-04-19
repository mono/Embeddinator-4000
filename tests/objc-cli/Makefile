TOP=../..

include $(TOP)/tests/managed-ios/Makefile.include

all: run-test perf xctest test-leaks test-static test-dynamic test-framework-ios xctest-ios-simulator

MONO=/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono

OBJC_GEN_DIR=../../objcgen
OBJC_GEN=$(OBJC_GEN_DIR)/bin/Debug/objcgen.exe
$(OBJC_GEN): $(wildcard $(OBJC_GEN_DIR)/*.cs) $(wildcard ../../support/*)
	$(MAKE) -C $(OBJC_GEN_DIR)

MANAGED_DLL_DIR=../managed
MANAGED_DLL=$(MANAGED_DLL_DIR)/bin/Debug/managed.dll
$(MANAGED_DLL): $(wildcard $(MANAGED_DLL_DIR)/*.cs)
	make -C $(MANAGED_DLL_DIR)

%/managed.dll: $(MANAGED_DLL)
	mkdir -p $(dir $@)
	cp $< $@

debug/libmanaged-ios-framework/managed-ios.framework: $(TESTS_MANAGED_IOS_DLL) $(OBJC_GEN)
	$(MONO) --debug $(abspath $(OBJC_GEN)) --debug $(abspath $<) -c --outdir=$(abspath $(dir $@)) --target=framework --platform=ios

debug/libmanaged.dylib: debug/managed.dll $(OBJC_GEN)
	$(MONO) --debug $(OBJC_GEN) --debug debug/managed.dll -c -o debug

release/libmanaged.dylib: release/managed.dll $(OBJC_GEN)
	$(MONO) --debug $(OBJC_GEN) release/managed.dll -c -o release

debug/test-cli: test-managed.m debug/libmanaged.dylib
	clang -g -O0 $< -lmanaged -Ldebug -Idebug -framework Foundation -o $@ -rpath @executable_path -fobjc-arc

release/test-cli: test-managed.m release/libmanaged.dylib
	clang -O2 $< -lmanaged -Lrelease -Irelease -framework Foundation -o $@ -rpath @executable_path -fobjc-arc

debug/perf-cli: perf-test.m debug/libmanaged.dylib
	clang -g -O0 $< -lmanaged -Ldebug -Idebug -framework Foundation -o $@ -rpath @executable_path -fobjc-arc

release/perf-cli: perf-test.m release/libmanaged.dylib
	clang -O2 $< -lmanaged -Lrelease -Irelease -framework Foundation -o $@ -rpath @executable_path -fobjc-arc

run-%-cli-test: %/test-cli
	@echo "Test ($*)"
	./$<

run-%-perf-test: %/perf-cli
	@echo "Performance ($*)"
	time ./$<

run-test: run-debug-cli-test run-release-cli-test

perf: run-debug-perf-test run-release-perf-test

clean:
	@rm -rf debug release

xctest: debug/libmanaged.dylib
	xcodebuild test -project libmanaged/*.xcodeproj -scheme Tests

test-leaks: test-cli-leaks test-perf-leaks test-xctest-leaks

../leaktest/bin/Debug/leaktest.exe: $(wildcard ../leaktest/*.cs) libLeakCheckAtExit.dylib
	/Library/Frameworks/Mono.framework/Versions/Current/Commands/msbuild /verbosity:quiet /nologo ../leaktest/leaktest.csproj

test-cli-leaks: debug/test-cli ../leaktest/bin/Debug/leaktest.exe
	mono --debug ../leaktest/bin/Debug/leaktest.exe $(abspath $<)

test-perf-leaks: debug/perf-cli ../leaktest/bin/Debug/leaktest.exe
	mono --debug ../leaktest/bin/Debug/leaktest.exe $(abspath $<)

test-xctest-leaks: ../leaktest/bin/Debug/leaktest.exe $(MANAGED_DLL) debug/libmanaged.dylib
	rm -Rf libmanaged/Tests.xctest
	xcodebuild -quiet build-for-testing -project libmanaged/*.xcodeproj -scheme Tests CONFIGURATION_BUILD_DIR=$(abspath libmanaged)
	mono --debug ../leaktest/bin/Debug/leaktest.exe `xcrun -f xctest` -XCTest All $(abspath libmanaged/Tests.xctest)

libLeakCheckAtExit.dylib: leak-at-exit.c
	clang -arch i386 -arch x86_64 -shared $< -o $@

test-static: test-static-macos test-static-ios test-static-tvos test-static-watchos

test-static-%: $(MANAGED_DLL) $(OBJC_GEN)
	/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono --debug $(OBJC_GEN) --debug $(MANAGED_DLL) -c --outdir=build/$@-temp-dir --target=staticlibrary --platform=$*

test-dynamic: test-dynamic-macos test-dynamic-ios test-dynamic-tvos test-dynamic-watchos

test-dynamic-%: $(MANAGED_DLL) $(OBJC_GEN)
	/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono --debug $(OBJC_GEN) --debug $(MANAGED_DLL) -c --outdir=build/$@-temp-dir --target=dylib --platform=$*

test-framework: test-framework-macos test-framework-ios test-framework-tvos test-framework-watchos

test-framework-%: debug/managed.dll $(OBJC_GEN)
	/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono --debug $(abspath $(OBJC_GEN)) --debug $(abspath debug/managed.dll) -c --outdir=$(abspath build/$@-temp-dir) --target=framework --platform=$*

xctest-ios-simulator: debug/libmanaged-ios-framework/managed-ios.framework
	xcodebuild -quiet test -project libmanaged-ios/libmanaged-ios.xcodeproj -scheme libmanaged-ios -destination 'platform=iOS Simulator,name=iPhone 6,OS=latest'

xctest-ios-device: build/ios-device-id debug/libmanaged-ios-framework/managed-ios.framework
	xcodebuild test -project libmanaged-ios/libmanaged-ios.xcodeproj -scheme libmanaged-ios -destination 'platform=iOS,id=$(shell cat build/ios-device-id)'

build/device-list.xml:
	@mkdir -p build
	/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/bin/mlaunch --listdev=$@ --output-format=xml

build/ios-device-id: build/device-list.xml
	csharp -e 'var doc = new System.Xml.XmlDocument (); doc.Load ("$<"); Console.WriteLine (doc.SelectSingleNode ("/MTouch/Device[DeviceClass = \"iPhone\"]/DeviceIdentifier").InnerText);' > $@.tmp
	mv $@.tmp $@
