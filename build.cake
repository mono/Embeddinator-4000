#!mono .cake/Cake/Cake.exe
#tool "nuget:?package=NUnit.ConsoleRunner&version=3.6.1"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

#load "build/Utils.cake"
#load "build/Android.cake"
#load "build/Tests.cake"
#load "build/Packaging.cake"

Task("Clean")
    .Does(() =>
    {
        CleanDirectory("./build/lib");
        CleanDirectory("./build/obj");
        CleanDirectories("./tests/common/c");
        CleanDirectories("./tests/common/mk");

        DeleteDirectories(GetDirectories("./tests/**/obj"), new DeleteDirectorySettings { Recursive = true });
        DeleteDirectories(GetDirectories("./tests/**/bin"), new DeleteDirectorySettings { Recursive = true });
        CleanDirectories(GetDirectories("./tests/android/**/build"));
    });

Task("NuGet-Restore")
    .Does(() =>
    {
        NuGetRestore("./Embeddinator-4000.sln");
    });

Task("Build-Binder")
    .IsDependentOn("Clean")
    .IsDependentOn("Generate-Project-Files")
    .IsDependentOn("NuGet-Restore")
    .Does(() =>
    {
        MSBuild("./build/projects/Embeddinator-4000.csproj", MSBuildSettings());
    });

Task("Generate-Project-Files")
    .Does(() =>
    {
        var os = IsRunningOnWindows() ? "windows" : IsRunningOnMacOS() ? "macosx" : "linux";
        Premake(File("./build/premake5.lua"), $"--outdir=.. --os={os}", "vs2015");
    });

Task("Default")
    .IsDependentOn("Build-Binder");

Task("Android-Tests")
    .IsDependentOn("Generate-Android")
    .IsDependentOn("Generate-Android-PCL")
    .IsDependentOn("Generate-Android-NetStandard")
    .IsDependentOn("Generate-Android-FSharp");

Task("Tests")
    .IsDependentOn("Android-Tests")
    .IsDependentOn("Build-CSharp-Tests")
    .IsDependentOn("Run-C-Tests")
    .IsDependentOn("Run-Java-Tests");

Task("Jenkins")
    .IsDependentOn("Build-Binder")
    .IsDependentOn("Android-Tests")
    .IsDependentOn("Build-CSharp-Tests")
    .IsDependentOn("Run-C-Tests")
    .IsDependentOn("Build-Java-Tests");

Task("Travis")
    .IsDependentOn("Build-Binder")
    .IsDependentOn("Build-CSharp-Tests")
    .IsDependentOn("Run-C-Tests")
    .IsDependentOn("Run-Java-Tests");

RunTarget(target);
