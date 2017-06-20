#addin nuget:?package=Cake.DoInDirectory

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var buildDir = Directory("./build/lib") + Directory(configuration);

Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildDir);
});

Task("Build-Binder")
    .Does(() =>
    {
        MSBuild("./build/projects/MonoEmbeddinator4000.csproj", settings => settings.SetConfiguration(configuration).SetVerbosity(Verbosity.Minimal));
    });

Task("Build-Managed")
    .Does(() =>
{
    MSBuild("./tests/managed/generic/managed-generic.csproj", settings => settings.SetConfiguration(configuration).SetVerbosity(Verbosity.Minimal));
});

Task("Generate-C")
    .IsDependentOn("Build-Binder")
    .IsDependentOn("Build-Managed")
    .Does(() =>
    {
        var managedDll = Directory("../../../tests/managed/generic/bin") + Directory(configuration) + File("managed.dll");

        DoInDirectory(buildDir, () =>
        {
            var exitCode = StartProcess("MonoEmbeddinator4000.exe", $"-gen=c -out=c -platform=Windows -compile -target=shared {managedDll}");
            if (exitCode != 0)
                throw new Exception("MonoEmbeddinator4000.exe failed!");
        });
        
    });

Task("Default")
    .IsDependentOn("Generate-C");

RunTarget(target);
