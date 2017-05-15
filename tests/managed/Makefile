TOP=../..

all:

include Makefile.include

ifeq ($(OS),Windows_NT)
all: $(GENERIC_MANAGED_DLL)
else
all: $(GENERIC_MANAGED_DLL) $(IOS_MANAGED_DLL)
endif

clean:
	@rm -rf bin/ obj/ */bin */obj subscripts.cs
