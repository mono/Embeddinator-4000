#addin nuget:?package=Cake.DoInDirectory

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var buildDir = Directory("./build/lib") + Directory(configuration);
var embeddinator = File("MonoEmbeddinator4000.exe");
var managedDll = Directory("../../../tests/managed/generic/bin") + Directory(configuration) + File("managed.dll");

//Java settings
var javaHome = EnvironmentVariable("JAVA_HOME");
if (string.IsNullOrEmpty(javaHome))
    throw new Exception("Could not find JAVA_HOME!");
var classPath = string.Join(";", new[]
{
    "./external/junit/hamcrest-core-1.3.jar",
    "./external/junit/junit-4.12.jar",
    buildDir + Directory("java"),
    buildDir + File("java/managed.jar"),
});

void Exec(string path, string args)
{
    var exitCode = StartProcess(path, args);
    if (exitCode != 0)
        throw new Exception(path + " failed!");
}

Task("Clean")
    .Does(() =>
    {
        CleanDirectory(buildDir);
    });

Task("Build-Binder")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        MSBuild("./build/projects/MonoEmbeddinator4000.csproj", settings => settings.SetConfiguration(configuration).SetVerbosity(Verbosity.Minimal));
    });

Task("Build-Managed")
    .IsDependentOn("Clean")
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
            var platform = IsRunningOnWindows() ? "Windows" : "macOS";
            Exec(embeddinator, $"-gen=c -out=c -platform={platform} -compile -target=shared {managedDll}");
        });
    });

Task("Generate-Java")
    .IsDependentOn("Build-Binder")
    .IsDependentOn("Build-Managed")
    .Does(() =>
    {
        DoInDirectory(buildDir, () =>
        {
            var platform = IsRunningOnWindows() ? "Windows" : "macOS";
            Exec(embeddinator, $"-gen=Java -out=java -platform={platform} -compile -target=shared {managedDll}");
        });
    });

Task("Generate-Android")
    .IsDependentOn("Build-Binder")
    .IsDependentOn("Build-Managed")
    .Does(() =>
    {
        DoInDirectory(buildDir, () =>
        {
            Exec(embeddinator, $"-gen=c -out=c -platform=Android -compile -target=shared {managedDll}");
        });
    });

Task("Build-Java-Tests")
    .IsDependentOn("Generate-Java")
    .Does(() =>
    {
        var output = buildDir + Directory("java");
        var tests = "./tests/common/java/mono/embeddinator/Tests.java";
        var javac = Directory(javaHome) + File("bin/javac");
        Exec(javac, $"-cp {classPath} -d {output} -Xdiags:verbose -Xlint:deprecation {tests}");
    });

Task("Run-Java-Tests")
    .IsDependentOn("Build-Java-Tests")
    .Does(() =>
    {
        var java = Directory(javaHome) + File("bin/java");
        Exec(java, $"-cp {classPath} -Djna.dump_memory=true org.junit.runner.JUnitCore mono.embeddinator.Tests");
    });

Task("Default")
    .IsDependentOn("Generate-Android");

RunTarget(target);
