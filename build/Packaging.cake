#!mono .cake/Cake/Cake.exe

Task("Create-Package")
    .IsDependentOn("Build-Binder")
    .Does(() =>
    {
        var objcgenBuildDir = Directory("./objcgen/bin/") + Directory(configuration);

        var files = new []
        {
            new NuSpecContent { Source = objcgenBuildDir.ToString() + "/*.exe", Target = "tools/" },
            new NuSpecContent { Source = objcgenBuildDir.ToString() + "/*.dll", Target = "tools/" },
            new NuSpecContent { Source = objcgenBuildDir.ToString() + "/*.pdb", Target = "tools/" },
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
        var apiKey = System.IO.File.ReadAllText ("./.nuget/.nugetapikey");
        var nupkg = "./build/Embeddinator-4000." + version + ".nupkg";

        NuGetPush(nupkg, new NuGetPushSettings
        {
            Verbosity = NuGetVerbosity.Detailed,
            Source = "https://www.nuget.org/api/v2/package",
            ApiKey = apiKey
        });
    });