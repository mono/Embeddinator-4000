#addin nuget:?package=Cake.DoInDirectory

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var buildDir = Directory("./build/lib") + Directory(configuration);
var embeddinator = File("MonoEmbeddinator4000.exe");
var managedDll = Directory("../../../tests/managed/generic/bin") + Directory(configuration) + File("managed.dll");

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
        DoInDirectory(buildDir, () =>
        {
            var exitCode = StartProcess(embeddinator, $"-gen=c -out=c -platform=Windows -compile -target=shared {managedDll}");
            if (exitCode != 0)
                throw new Exception(embeddinator + " failed!");
        });
    });

Task("Generate-Java")
    .IsDependentOn("Generate-C")
    .Does(() =>
    {
        DoInDirectory(buildDir, () =>
        {
            var exitCode = StartProcess(embeddinator, $"-gen=Java -out=java -platform=Windows -compile -target=shared {managedDll}");
            if (exitCode != 0)
                throw new Exception(embeddinator + " failed!");
        });
    });

Task("Default")
    .IsDependentOn("Generate-Java");

RunTarget(target);
