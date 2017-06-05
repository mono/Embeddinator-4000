all: run-tests

MONO=/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono
MONOMSBUILD=/Library/Frameworks/Mono.framework/Versions/Current/Commands/msbuild
OBJC_GEN_DIR=../../objcgen
OBJC_GEN=$(OBJC_GEN_DIR)/bin/Debug/objcgen.exe
$(OBJC_GEN): $(wildcard $(OBJC_GEN_DIR)/*.cs) $(wildcard ../../support/*)
	$(MAKE) -C $(OBJC_GEN_DIR)

# Add here the name of the folder containing the tests
TESTS = GenericsTest EnumsTest NoInitInSubclassTest
.PHONY: clean $(TESTS)

define FindGenErrWarnCodeTemplate
$(1):
	@echo "$1";
	@$(MONOMSBUILD) /p:Configuration=$(3) $(1)/$(2).csproj /p:OutputPath=DllOutput $(if $(V),,> /dev/null)
	@$(MONO) $(OBJC_GEN) $(if $(filter $(3),Debug),--debug,) -c $(1)/DllOutput/$(2).dll -o $(1)/E4KOutput 2>&1 | $(if $(V),grep --color=always EM$(4),grep EM$(4) > /dev/null)
endef

define DoNotFindGenErrWarnCodeTemplate
$(1):
	@echo "$1"
	@$(MONOMSBUILD) /p:Configuration=$(3) $(1)/$(2).csproj /p:OutputPath=DllOutput $(if $(V),,> /dev/null)
	@!($(MONO) $(OBJC_GEN) $(if $(filter $(3),Debug),--debug,) -c $(1)/DllOutput/$(2).dll -o $(1)/E4KOutput 2>&1 | grep EM$(4) $(if $(V),,> /dev/null))
	$(if $(V),@echo "$1: Success EM$(4) not found";,)
endef

define FindXcodeErrWarnCodeTemplate
$(1):
	@echo "$1";
	@$(MONOMSBUILD) /p:Configuration=$(5) $(1)/$(2).csproj /p:OutputPath=DllOutput $(if $(V),,> /dev/null)
	@$(MONO) $(OBJC_GEN) $(if $(filter $(5),Debug),--debug,) -c $(1)/DllOutput/$(2).dll -o $(1)/E4KOutput $(if $(V),,> /dev/null)
	@xcodebuild -project $(1)/$(3).xcodeproj -target $(4) -configuration $(5) 2>&1 | grep $(6) $(if $(V),,> /dev/null)
endef

# {Template},{GENERATOR_TEST},{csprojName},{Debug|Release},{[warn|error]code}
$(eval $(call FindGenErrWarnCodeTemplate,GenericsTest,GenericsLib,Debug,1010))
$(eval $(call DoNotFindGenErrWarnCodeTemplate,EnumsTest,EnumsLib,Release,1010))

# {Template},{GENERATOR_TEST},{csprojName},{xcodeprojName},{target},{Debug|Release},{expected[warn|error]}
$(eval $(call FindXcodeErrWarnCodeTemplate,NoInitInSubclassTest,ConstructorsLib,NoInitInSubclassTest,NoInitInSubclassTest,Release,"error: 'initWithId:' is unavailable"))

run-tests: $(OBJC_GEN) $(TESTS)

clean:
	@find . -name "DllOutput" -type d -prune -exec rm -r "{}" +
	@find . -name "E4KOutput" -type d -prune -exec rm -r "{}" +
	@find . -name "obj" -type d -prune -exec rm -r "{}" +
	@find . -name "build" -type d -prune -exec rm -r "{}" +