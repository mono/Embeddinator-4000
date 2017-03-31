run-tests:
	nuget restore ../../generator.sln
	msbuild objcgentest.csproj
	mono --debug ../../packages/NUnit.ConsoleRunner.3.6.1/tools/nunit3-console.exe bin/Debug/objcgentest.dll
