ifeq ($(wildcard /usr/local/bin/colordiff),) 
COMPARE_COMMAND = diff -u
else 
COMPARE_COMMAND = colordiff -u
endif 

help::
	@echo "make baseline - compile managed.dll and save the output for comparision"
	@echo "make compare - compile maanged.dll and compare to the saved output"

clean::
	rm -rf output
	rm -rf saved

compare::
	rm -rf output
	mkdir -p output
	mono ../../objcgen/bin/Debug/objcgen.exe -o output/ ../../tests/managed/generic/bin/Debug/managed.dll
	$(COMPARE_COMMAND) saved/bindings.h output/bindings.h
	$(COMPARE_COMMAND) saved/bindings.m output/bindings.m

baseline::
	rm -rf saved
	msbuild ../../tests/managed/generic/managed-generic.csproj /t:Clean
	msbuild ../../tests/managed/generic/managed-generic.csproj
	mkdir -p saved
	mono ../../objcgen/bin/Debug/objcgen.exe -o saved/ ../../tests/managed/generic/bin/Debug/managed.dll
