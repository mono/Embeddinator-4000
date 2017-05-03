all: bin/Debug/objcgen.exe shellcheck

OBJCGEN_FILES := \
	$(shell grep "Compile Include" *.csproj | grep -v Version.generated.cs | sed 's/.*Include="\(.*\)".*/\1/' | sed 's_\\_/_'g) \
	$(shell grep "EmbeddedResource Include" *.csproj | sed 's/.*Include="\(.*\)".*/\1/' | sed 's_\\_/_'g) \

bin/Debug/objcgen.exe: $(OBJCGEN_FILES)
	nuget restore ../generator.sln
	msbuild objcgen.csproj

SHELLCHECK:=$(shell which shellcheck)
shellcheck: thin-framework.sh
ifeq ($(SHELLCHECK),)
	@echo No shellcheck tool found
else
	$(SHELLCHECK) $^
endif

VERSION = 0.1
FRAMEWORK_DIR = _build/Library/Frameworks/Xamarin.Embeddinator-4000
INSTALLER_FILES = Xamarin.Embeddinator-4000-$(VERSION).pkg

package:: bin/Debug/objcgen.exe
	mkdir -p $(FRAMEWORK_DIR)/Commands/
	mkdir -p $(FRAMEWORK_DIR)/Versions/$(VERSION)/bin
	ln -fs $(VERSION) $(FRAMEWORK_DIR)/Versions/Current
	cp bin/Debug/objcgen.exe $(FRAMEWORK_DIR)/Versions/Current/bin/
	cp bin/Debug/Mono.Options.dll $(FRAMEWORK_DIR)/Versions/Current/bin/
	cp script/objcgen  $(FRAMEWORK_DIR)/Commands/
	chmod +x $(FRAMEWORK_DIR)/Commands/objcgen

clean-package::
	rm -rf _build/
	rm -rf _installer/


$(INSTALLER_FILES): package
	rm -rf _installer/
	mkdir -p _installer/ROOT/
	cp -R _build/ _installer/ROOT/
	pkgbuild --root _installer/ROOT --identifier com.xamarin.embeddinator-4000 --version 0.1 $(INSTALLER_FILES)

installer:: $(INSTALLER_FILES)
