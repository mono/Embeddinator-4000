all: bin/Debug/objcgen.exe shellcheck

-include Make.config.inc

PACKAGE_VERSION := $(shell grep "var version" ../build/Packaging.cake | cut -d \" -f 4)
PACKAGE_FULLVERSION=$(PACKAGE_VERSION).$(COMMIT_DISTANCE)

Make.config.inc: Makefile
	@rm -f $@
	@printf "COMMIT_DISTANCE:=$(shell LANG=C; export LANG && git log `git blame Makefile HEAD | grep PACKAGE_VERSION= | sed 's/ .*//' `..HEAD --oneline | wc -l | sed 's/ //g')\n" >> $@
	@printf "VERSION:=$(PACKAGE_VERSION).$(shell LANG=C; export LANG && git log `git blame Makefile HEAD | grep PACKAGE_VERSION= | sed 's/ .*//' `..HEAD --oneline | wc -l | sed 's/ //g')\n" >> $@

OBJCGEN_FILES := \
	$(shell grep "Compile Include" *.csproj | grep -v Version.generated.cs | sed 's/.*Include="\(.*\)".*/\1/' | sed 's_\\_/_'g) \
	$(shell grep "EmbeddedResource Include" *.csproj | sed 's/.*Include="\(.*\)".*/\1/' | sed 's_\\_/_'g) \

bin/Debug/objcgen.exe: $(OBJCGEN_FILES)
	nuget restore ../Embeddinator-4000.sln
	msbuild objcgen.csproj

SHELLCHECK:=$(shell which shellcheck)
shellcheck: thin-framework.sh
ifeq ($(SHELLCHECK),)
	@echo No shellcheck tool found
else
	$(SHELLCHECK) $^
endif

E4K_FRAMEWORK_DIR = /Library/Frameworks/Xamarin.Embeddinator-4000.framework
NUGET_DIR = _build/

nuget-prep:: bin/Debug/objcgen.exe Make.config.inc
	mkdir -p $(NUGET_DIR)
	cp bin/Debug/objcgen.{exe,pdb} $(NUGET_DIR)
	cp bin/Debug/Mono.Options.dll $(NUGET_DIR)
	cp bin/Debug/IKVM.Reflection.dll $(NUGET_DIR)
	cp script/objcgen  $(NUGET_DIR)
	chmod +x $(NUGET_DIR)objcgen

nuget::
	cd .. && ./build.sh -t Create-Package

nuget-install:: nuget
	cp ../build/Embeddinator-4000.$(PACKAGE_VERSION).nupkg ~/nuget

clean-nuget::
	rm -rf _build/
	rm Make.config.inc

clean:: clean-nuget
	msbuild objcgen.csproj /t:clean
	rm -rf ./bin
	rm -rf ./obj
