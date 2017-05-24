all: unitsnet
	
TOP=../..
OBJC_GEN=$(OBJC_GEN_DIR)/bin/Debug/objcgen.exe

MONO=/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono

OBJC_GEN_DIR=../../objcgen
OBJC_GEN=$(OBJC_GEN_DIR)/bin/Debug/objcgen.exe
$(OBJC_GEN): $(wildcard $(OBJC_GEN_DIR)/*.cs) $(wildcard ../../support/*)
	$(MAKE) -C $(OBJC_GEN_DIR)

UNITSNET_VERSION=3.58.0

unitsnet: $(OBJC_GEN)
	nuget install UnitsNet -Version $(UNITSNET_VERSION)
	$(MONO) $(OBJC_GEN) UnitsNet.$(UNITSNET_VERSION)/lib/netstandard1.0/UnitsNet.dll -debug -o Debug -c -nowarn:1032,1033,1034

clean:
	rm -rf UnitsNet.*/ Debug/ *.h *.c *.m
