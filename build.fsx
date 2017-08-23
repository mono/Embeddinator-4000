
#I @"packages/NUnit.ConsoleRunner/tools"
#I @"packages/FAKE/tools/"
#r @"FakeLib.dll"
#r @"nunit.engine.api.dll"
#r @"nunit.engine.dll"
open Fake
open Fake.FileHelper
open Fake.FileUtils

let configuration = getBuildParamOrDefault "configuration" "Release"

MSBuildDefaults <- 
    { MSBuildDefaults with
        Verbosity = Some MSBuildVerbosity.Minimal
        Properties = 
            [ 
                "Configuration", configuration
                "Platform", "AnyCPU"
            ]
        NoLogo = true
    }

module Tests = 
    open System.IO
    let private pathToTests = currentDirectory @@ "tests"
    let private pathToManagedTests = pathToTests @@ "managed"
    let private managedDll = pathToManagedTests @@ "generic" @@ "bin" @@ configuration @@ "managed.dll"

    Target "Build-Managed" (fun _ ->
        let path = pathToManagedTests @@ "generic" @@ "managed-generic.csproj"
        !! path
        |> MSBuildWithDefaults "" 
        |> Log "Output: "
    )

    Target "Build-Android" (fun _ ->
        let path = pathToManagedTests @@ "android" @@ "managed-android.csproj"
        !! path
        |> MSBuildWithDefaults "" 
        |> Log "Output: "
    )

    Target "Build-FSharp-Android" (fun _ ->
        let path = pathToManagedTests @@ "fsharp-android" @@ "fsharp-android.fsproj"
        !! path
        |> MSBuildWithDefaults ""
        |> Log "Output: "
    )

    Target "Build-PCL" (fun _ ->
        let path = pathToManagedTests @@ "pcl" @@ "managed-pcl.csproj"
        !! path
        |> MSBuildWithDefaults ""
        |> Log "Output: "
    )

    Target "Build-NetStandard" (fun _ ->
        let path = pathToManagedTests @@ "netstandard" @@ "managed-netstandard.csproj"
        !! path
        |> MSBuildWithDefaults ""
        |> Log "Output: "
    )

    Target "Build-CSharp-Tests" (fun _ ->
        let path = pathToTests @@ "MonoEmbeddinator4000.Tests" @@ "MonoEmbeddinator4000.Tests.csproj"
        !! path
        |> MSBuildWithDefaults ""
        |> Log "Output: "
    )

module Utils =
    open System.IO
    open System

    let private premake = 
        let exe = 
            if isWindows then "premake5.exe" 
            elif isMacOS then "premake5-osx"
            elif isLinux then "premake5"
            else 
                traceError "Unsupported operating system detected!"
                ""
        Path.Combine("external", "CppSharp", "build", exe)
    
    let private os = 
        if isWindows then "windows" 
        elif isMacOS then "macosx"
        elif isLinux then "linux"
        else 
            traceError "Unsupported operating system detected!"
            ""

    let ExecutePremake file args action =
        ExecProcess (fun info ->
            info.FileName <- premake
            info.WorkingDirectory <- "."
            info.Arguments <- sprintf "--file=%s %s %s" file args action
        ) TimeSpan.MaxValue
    
    let ExecutePremakeCurrOs file args action =
        ExecutePremake file (sprintf "--os=%s %s" os args) action

open Utils
open Tests

Target "Clean" (fun _ ->
    trace "Cleaning"
    !! "./build/lib"
    ++ "./build/obj"
    ++ "./tests/common/c"
    ++ "./tests/common/mk"
    ++ "./test/**/obj"
    ++ "./tests/**/bin" 
    ++ "./tests/android/**/build"
    |> CleanDirs
)

Target "Build-Binder" (fun _ -> 
    !! "./build/projects/Embeddinator-4000.csproj"
        |> MSBuildWithDefaults ""
        |> Log "Output:"
)

Target "Generate-Project-Files" (fun _ ->
    ExecutePremakeCurrOs "./build/premake5.lua" "--outdir=.." "vs2015"
    |> sprintf "%d"
    |> traceEndTarget
)

Target "Default" ignore

Target "Tests" ignore

Target "AppVeyor" ignore

Target "Travis" ignore

"Build-Binder"
    ==> "Clean"
    ==> "Generate-Project-Files"

"Default"
    ==> "Build-Binder"

"Test"
    ==> "Generate-Android"
    ==> "Generate-Android-PCL"
    ==> "Generate-Android-NetStandard"
    ==> "Generate-Android-FSharp"
    ==> "Build-CSharp-Tests"
    ==> "Run-C-Tests"

"Travis"
    ==> "Build-Binder"
    ==> "Build-CSharp-Tests"
    ==> "Run-C-Tests"

RunTargetOrDefault "Default"