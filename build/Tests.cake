var managedDll = Directory("./tests/managed/generic/bin") + Directory(configuration) + File("managed.dll");
var fsharpManagedDll = Directory("./tests/managed/fsharp-generic/bin") + Directory(configuration) + File("fsharpManaged.dll");

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

Task("Build-FSharp-Generic")
    .IsDependentOn("Clean")
    .IsDependentOn("NuGet-Restore")
    .Does(()=>
    {
        MSBuild("./tests/managed/fsharp-generic/fsharp-generic.fsproj", settings =>
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

var commonDir = Directory("./tests/common");
var mkDir = commonDir + Directory("mk");

Task("Generate-C")
    .IsDependentOn("Build-Binder")
    .IsDependentOn("Build-Managed")
    .IsDependentOn("Build-FSharp-Generic")
    .Does(() =>
    {
        var platform = IsRunningOnWindows() ? "Windows" : IsRunningOnMacOS() ? "macOS" : "Linux";
        var output = commonDir + Directory("c");
        Exec(embeddinator, $"-gen=c -out={output} -platform={platform} -target=shared -verbose {managedDll} {fsharpManagedDll}");
    });

Task("Build-C-Tests")
    .IsDependentOn("Generate-C")
    .Does(() =>
    {
        // Generate native project build files using Premake.
        var os = IsRunningOnWindows() ? "--os=windows" : IsRunningOnMacOS() ? "--os=macosx" : "--os=linux";
        var action = IsRunningOnWindows() ? "vs2015" : "gmake";
        Premake(commonDir + File("premake5.lua"), os, action);

        // Execute the build files.
        if (IsRunningOnWindows())
            MSBuild(mkDir + File("mk.sln"), settings =>
                settings.SetConfiguration(configuration).SetVerbosity(Verbosity.Minimal)
                        .SetPlatformTarget(PlatformTarget.Win32));
        else
        {
            var envVars = new Dictionary<string, string> ();
            envVars.Add("config", configuration.ToLowerInvariant());

            if (TravisCI.IsRunningOnTravisCI)
                envVars.Add("verbose", "true");

            var settings = new ProcessSettings
            {
                Arguments = $"-C {mkDir}",
                EnvironmentVariables = envVars
            };
            Exec("make", settings);
        }

        // Copy the managed test DLL to the output folder.
        System.IO.File.Copy(managedDll, $"{mkDir}/bin/{configuration}/" +
            System.IO.Path.GetFileName(managedDll));
        System.IO.File.Copy(fsharpManagedDll, $"{mkDir}/bin/{configuration}/" +
            System.IO.Path.GetFileName(fsharpManagedDll));

        if (IsRunningOnWindows())
        {
            var monoDir = @"C:\Program Files (x86)\Mono";
            var monoLib = "mono-2.0-sgen.dll";

            // Copy the Mono runtime DLL to the output folder.
            System.IO.File.Copy($"{monoDir}\\bin\\{monoLib}", $"{mkDir}\\bin\\{configuration}\\{monoLib}");

            // Create a symbolic link to the Mono class libraries directory.
            Exec("cmd.exe", $"/c mklink /D \"{mkDir}\\bin\\lib\" \"{monoDir}\\lib\"");
        }
    });

Task("Run-C-Tests")
    .IsDependentOn("Build-C-Tests")
    .Does(() =>
    {
        var binDir = Directory($"./{mkDir}/bin/{configuration}");
        Exec(binDir + File("common.Tests" + (IsRunningOnWindows() ? ".exe" : string.Empty)));
    });

/// ---------------------------
/// Java tests
/// ---------------------------

Task("Generate-Java")
    .IsDependentOn("Build-Binder")
    .IsDependentOn("Build-Managed")
    .Does(() =>
    {
        var platform = IsRunningOnWindows() ? "Windows" : IsRunningOnMacOS() ? "macOS" : "Linux";
        var output = mkDir + Directory("java");
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
else if (FileExists("/usr/libexec/java_home"))
{
    using (var process = StartAndReturnProcess("/usr/libexec/java_home", new ProcessSettings { RedirectStandardOutput = true }))
    {
        process.WaitForExit();

        javaHome = process.GetStandardOutput().First().Trim();
    }
}
else
{
    javaHome = EnvironmentVariable("JAVA_HOME");
}

var classPath = string.Join(IsRunningOnWindows() ? ";" : ":", new[]
{
    "./external/junit/hamcrest-core-1.3.jar",
    "./external/junit/junit-4.12.jar",
    mkDir + Directory("java"),
    mkDir + File("java/managed.jar"),
});

Task("Build-Java-Tests")
    .IsDependentOn("Generate-Java")
    .Does(() =>
    {
        var output = mkDir + Directory("java");
        var tests = File("./tests/common/java/mono/embeddinator/Tests.java");
        var javac = Directory(javaHome) + File("bin/javac");
        Exec(javac, $"-cp {classPath} -d {output} -Xdiags:verbose -Xlint:deprecation -Xlint:unchecked {tests}");
    });

Task("Run-Java-Tests")
    .IsDependentOn("Build-Java-Tests")
    .Does(() =>
    {
        var java = Directory(javaHome) + File("bin/java");
        Exec(java, $"-cp {classPath} -Djna.dump_memory=true -Djna.nosys=true org.junit.runner.JUnitCore mono.embeddinator.Tests");
    });
