#!mono .cake/Cake/Cake.exe

var version = Argument("version", "0.5.0");

Task("Build-ObjC")
    .Does(() =>
    {
        if (!IsRunningOnWindows ()) {
            var exitCode = StartProcess("make", new ProcessSettings {
                Arguments = new ProcessArgumentBuilder().Append ("-C").Append ("objcgen/").Append ("nuget-prep")
            });
            if (exitCode != 0)
                throw new Exception ($"make nuget-prep in objcgen/ somehow failed: {exitCode}");
        }
    });

Task("Create-Package")
    .IsDependentOn("Build-Binder")
    .IsDependentOn("Download-Xamarin-Android")
    .IsDependentOn("Build-ObjC")
    .Does(() =>
    {
        var objcgenBuildDir = Directory("./objcgen/_build/");

        var files = new []
        {
            new NuSpecContent { Source = objcgenBuildDir.ToString() + "/*", Target = "tools/" },
            new NuSpecContent { Source = buildDir.ToString() + "/*.exe", Target = "tools/" },
            new NuSpecContent { Source = buildDir.ToString() + "/*.dll", Target = "tools/" },
            new NuSpecContent { Source = buildDir.ToString() + "/*.pdb", Target = "tools/" },
            new NuSpecContent { Source = Directory("./external/jna").ToString() + "/**", Target = "external/jna" },
            new NuSpecContent { Source = Directory("./external/Xamarin.Android").ToString() + "/**", Target = "external/Xamarin.Android" },
            new NuSpecContent { Source = Directory("./support").ToString() + "/**", Target = "support/" },
        };

        var settings = new NuGetPackSettings
        {
            Verbosity = NuGetVerbosity.Detailed,
            Version = version,
            Files = files,
            OutputDirectory = Directory("./build"),
            NoPackageAnalysis = true
        };

        NuGetPack("./Embeddinator-4000.nuspec", settings);
    });

Task("Publish-Package")
    .Does(() =>
    {
        var apiKey = System.IO.File.ReadAllText ("./.cake/.nugetapikey");
        var nupkg = "./build/Embeddinator-4000." + version + ".nupkg";

        NuGetPush(nupkg, new NuGetPushSettings
        {
            Verbosity = NuGetVerbosity.Detailed,
            Source = "https://www.nuget.org/api/v2/package",
            ApiKey = apiKey
        });
    });
