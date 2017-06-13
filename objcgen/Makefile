all: bin/Debug/objcgen.exe shellcheck

-include Make.config.inc

PACKAGE_VERSION=0.3.0
PACKAGE_FULLVERSION=$(PACKAGE_VERSION).$(COMMIT_DISTANCE)

Make.config.inc: Makefile
	@rm -f $@
	@printf "COMMIT_DISTANCE:=$(shell LANG=C; export LANG && git log `git blame Makefile HEAD | grep PACKAGE_VERSION= | sed 's/ .*//' `..HEAD --oneline | wc -l | sed 's/ //g')\n" >> $@
	@printf "VERSION:=$(PACKAGE_VERSION).$(shell LANG=C; export LANG && git log `git blame Makefile HEAD | grep PACKAGE_VERSION= | sed 's/ .*//' `..HEAD --oneline | wc -l | sed 's/ //g')\n" >> $@

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

E4K_FRAMEWORK_DIR = /Library/Frameworks/Xamarin.Embeddinator-4000.framework
FRAMEWORK_DIR = _build/$(E4K_FRAMEWORK_DIR)
PATHS_DIR = _build/etc/paths.d
INSTALLER_FILES = Xamarin.Embeddinator-4000-$(PACKAGE_FULLVERSION).pkg

package:: bin/Debug/objcgen.exe Make.config.inc
	mkdir -p $(FRAMEWORK_DIR)/Commands/
	mkdir -p $(FRAMEWORK_DIR)/Versions/$(PACKAGE_FULLVERSION)/bin
	mkdir -p $(PATHS_DIR)
	echo "${E4K_FRAMEWORK_DIR}/Commands" >> "${PATHS_DIR}/embeddinator-4000-commands"
	ln -fs $(PACKAGE_FULLVERSION) $(FRAMEWORK_DIR)/Versions/Current
	cp bin/Debug/objcgen.{exe,pdb} $(FRAMEWORK_DIR)/Versions/Current/bin/
	cp bin/Debug/Mono.Options.dll $(FRAMEWORK_DIR)/Versions/Current/bin/
	cp script/objcgen  $(FRAMEWORK_DIR)/Commands/
	chmod +x $(FRAMEWORK_DIR)/Commands/objcgen

clean-package::
	rm -rf _build/
	rm -rf _installer/
	rm Make.config.inc

$(INSTALLER_FILES): package
	rm -rf _installer/
	mkdir -p _installer/ROOT/
	cp -R _build/ _installer/ROOT/
	pkgbuild --root _installer/ROOT --identifier com.xamarin.embeddinator-4000 --version $(PACKAGE_FULLVERSION) $(INSTALLER_FILES)

installer:: $(INSTALLER_FILES)
