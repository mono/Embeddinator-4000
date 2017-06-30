#!mono .cake/Cake/Cake.exe

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var buildDir = Directory("./build/lib") + Directory(configuration);
var embeddinator = buildDir + File("MonoEmbeddinator4000.exe");
var managedDll = Directory("./tests/managed/generic/bin") + Directory(configuration) + File("managed.dll");

//Java settings
string javaHome;
if (IsRunningOnWindows())
{
    javaHome = EnvironmentVariable("JAVA_HOME");
    if (string.IsNullOrEmpty(javaHome))
        throw new Exception("Cannot find Java SDK: JAVA_HOME environment variable is not set.");
}
else
{
    using (var process = StartAndReturnProcess("/usr/libexec/java_home", new ProcessSettings { RedirectStandardOutput = true }))
    {
        process.WaitForExit();

        javaHome = process.GetStandardOutput().First().Trim();
    }
}

var classPath = string.Join(IsRunningOnWindows() ? ";" : ":", new[]
{
    "./external/junit/hamcrest-core-1.3.jar",
    "./external/junit/junit-4.12.jar",
    buildDir + Directory("java"),
    buildDir + File("java/managed.jar"),
});

void Exec(string path, string args)
{
    var exitCode = IsRunningOnWindows() || !path.EndsWith(".exe") ? StartProcess(path, args) : StartProcess("mono", path + " " + args);
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
        var platform = IsRunningOnWindows() ? "Windows" : "macOS";
        var output = buildDir + Directory("c");
        Exec(embeddinator, $"-gen=c -out={output} -platform={platform} -compile -target=shared {managedDll}");
    });

Task("Generate-Java")
    .IsDependentOn("Build-Binder")
    .IsDependentOn("Build-Managed")
    .Does(() =>
    {
        var platform = IsRunningOnWindows() ? "Windows" : "macOS";
        var output = buildDir + Directory("java");
        Exec(embeddinator, $"-gen=Java -out={output} -platform={platform} -compile -target=shared {managedDll}");
    });

Task("Generate-Android")
    .IsDependentOn("Build-Binder")
    .IsDependentOn("Build-Managed")
    .Does(() =>
    {
        var output = buildDir + Directory("java");
        Exec(embeddinator, $"-gen=Java -out={output} -platform=Android -compile -target=shared {managedDll}");
    });

Task("Build-Java-Tests")
    .IsDependentOn("Generate-Java")
    .Does(() =>
    {
        var output = buildDir + Directory("java");
        var tests = File("./tests/common/java/mono/embeddinator/Tests.java");
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

void Premake(string file, string args, string action)
{
    var premakePath = Directory("./external/CppSharp/build/") + (IsRunningOnWindows() ?
        File("premake5.exe") : File("premake5-osx"));
    Exec(premakePath, $"--file={file} {args} {action}");
}

Task("Generate-Project-Files")
    .Does(() =>
    {
        Premake(File("./build/premake5.lua"), "--outdir=. --os=macosx", "vs2015");
    });

Task("Default")
    .IsDependentOn("Generate-Android");

RunTarget(target);
