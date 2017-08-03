var managedDll = Directory("./tests/managed/generic/bin") + Directory(configuration) + File("managed.dll");
var androidDll = Directory("./tests/managed/android/bin") + Directory(configuration) + File("managed.dll");
var pclDll = Directory("./tests/managed/pcl/bin") + Directory(configuration) + File("managed.dll");
var netStandardDll = Directory("./tests/managed/netstandard/bin") + Directory(configuration) + File("netstandard1.6/managed.dll");
var fsharpAndroidDll = Directory("./tests/managed/fsharp-android/bin") + Directory(configuration) + File("managed.dll");

/// ---------------------------
/// Managed test projects
/// ---------------------------

Task("Build-Managed")
    .IsDependentOn("Clean")
    .IsDependentOn("NuGet-Restore")
    .Does(() =>
    {
        MSBuild("./tests/managed/generic/managed-generic.csproj", settings =>
            settings.SetConfiguration(configuration).SetVerbosity(Verbosity.Minimal));
    });

Task("Build-Android")
    .IsDependentOn("Clean")
    .IsDependentOn("NuGet-Restore")
    .Does(() =>
    {
        MSBuild("./tests/managed/android/managed-android.csproj", settings =>
            settings.SetConfiguration(configuration).SetPlatformTarget(PlatformTarget.MSIL).SetVerbosity(Verbosity.Minimal));
    });

Task("Build-FSharp-Android")
    .IsDependentOn("Clean")
    .IsDependentOn("NuGet-Restore")
    .Does(() =>
    {
        MSBuild("./tests/managed/fsharp-android/fsharp-android.fsproj", settings =>
            settings.SetConfiguration(configuration).SetPlatformTarget(PlatformTarget.MSIL).SetVerbosity(Verbosity.Minimal));
    });

Task("Build-PCL")
    .IsDependentOn("Clean")
    .IsDependentOn("NuGet-Restore")
    .Does(() =>
    {
        MSBuild("./tests/managed/pcl/managed-pcl.csproj", settings =>
            settings.SetConfiguration(configuration).SetPlatformTarget(PlatformTarget.MSIL).SetVerbosity(Verbosity.Minimal));
    });

Task("Build-NetStandard")
    .IsDependentOn("Clean")
    .IsDependentOn("NuGet-Restore")
    .Does(() =>
    {
        var project = "./tests/managed/netstandard/managed-netstandard.csproj";
        DotNetCoreRestore(project);
        MSBuild(project, settings => settings.SetConfiguration(configuration).SetPlatformTarget(PlatformTarget.MSIL).SetVerbosity(Verbosity.Minimal));
    });

Task("Build-CSharp-Tests")
    .IsDependentOn("Build-Binder")
    .IsDependentOn("Build-Managed")
    .Does(() =>
    {
        MSBuild("./tests/MonoEmbeddinator4000.Tests/MonoEmbeddinator4000.Tests.csproj", settings => settings.SetConfiguration(configuration).SetVerbosity(Verbosity.Minimal));
    });

Task("Run-CSharp-Tests")
    .IsDependentOn("Build-CSharp-Tests")
    .Does(() =>
    {
        NUnit3($"./tests/MonoEmbeddinator4000.Tests/bin/{configuration}/MonoEmbeddinator4000.Tests.dll", new NUnit3Settings
        {
            NoResults = true
        });
    });

/// ---------------------------
/// C tests
/// ---------------------------

Task("Generate-C")
    .IsDependentOn("Build-Binder")
    .IsDependentOn("Build-Managed")
    .Does(() =>
    {
        var platform = IsRunningOnWindows() ? "Windows" : "macOS";
        var output = buildDir + Directory("c");
        Exec(embeddinator, $"-gen=c -out={output} -platform={platform} -compile -target=shared {managedDll}");
    });

Task("Build-C-Tests")
    .IsDependentOn("Generate-C")
    .Does(() =>
    {
        var commonDir = Directory("./tests/common");

        // Generate native project build files using Premake.
        var os = IsRunningOnWindows() ? "--os=windows" : "--os=macosx";
        var action = IsRunningOnWindows() ? "vs2015" : "gmake";
        Premake(commonDir + File("premake5.lua"), os, action);

        // Execute the build files.
        var mkDir = commonDir + Directory("mk");
        if (IsRunningOnWindows())
            MSBuild(mkDir + File("mk.sln"), settings =>
                settings.SetConfiguration(configuration).SetVerbosity(Verbosity.Minimal));
        else
            Exec("make", $"-C {mkDir}");
    });

Task("Run-C-Tests")
    .IsDependentOn("Build-C-Tests")
    .Does(() =>
    {
        var binDir = Directory("./tests/common/mk/bin/Debug");
        Exec(binDir + File("common.Tests"));
    });

/// ---------------------------
/// Java tests
/// ---------------------------

Task("Generate-Java")
    .IsDependentOn("Build-Binder")
    .IsDependentOn("Build-Managed")
    .Does(() =>
    {
        var platform = IsRunningOnWindows() ? "Windows" : "macOS";
        var output = buildDir + Directory("java");
        Exec(embeddinator, $"-gen=Java -out={output} -platform={platform} -compile -target=shared {managedDll}");
    });

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