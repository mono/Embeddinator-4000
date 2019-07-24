var buildDir = Directory("./build/lib") + Directory(configuration);

var androidDll = Directory("./tests/managed/android/bin") + Directory(configuration) + File("managed.dll");
var fsharpAndroidDll = Directory("./tests/managed/fsharp-android/bin") + Directory(configuration) + File("managed.dll");
var pclDll = Directory("./tests/managed/pcl/bin") + Directory(configuration) + File("managed.dll");
var netStandardDll = Directory("./tests/managed/netstandard/bin") + Directory(configuration) + File("netstandard1.6/managed.dll");

//NOTE: this is a temporary task for downloading a Jenkins build of Xamarin.Android
Task("Download-Xamarin-Android")
    .Does(() =>
    {
        var xamarinPath = Directory("./external/Xamarin.Android");
        if (DirectoryExists(xamarinPath))
        {
            Console.WriteLine("Xamarin.Android SDK already downloaded...");
            return;
        }

        Console.WriteLine("Downloading Xamarin.Android SDK, this will take a while...");

        // From https://jenkins.mono-project.com/view/Xamarin.Android/job/xamarin-android-freestyle/682/Azure/
        var artifact = "oss-xamarin.android_v8.0.0.37_Darwin-x86_64_HEAD_376f684";
        var url = $"https://xamjenkinsartifact.azureedge.net/xamarin-android/xamarin-android/{artifact}.zip";
        var temp = DownloadFile(url);
        var tempDir = temp.GetDirectory() + "/" + artifact;
        try
        {
            Console.WriteLine("Unzipping Xamarin.Android SDK...");

            //Unzip into root of %TEMP%
            //Root directory of zip will contain {artifact} as directory name
            Unzip(temp, temp.GetDirectory());

            //Copy bin/Release to final directory in ./external/Xamarin.Android
            CopyDirectory(Directory(tempDir) + Directory("./bin/Release"), xamarinPath);

            //There are some additional files we don't need for Embeddinator
            //Removing them should make our distribution smaller 875.6MB -> 277.2MB (92.7MB compressed)
            DeleteFiles(GetFiles("./external/Xamarin.Android/*"));
            DeleteDirectory("./external/Xamarin.Android/bin", true);
            DeleteDirectory("./external/Xamarin.Android/lib/mandroid", true);
            foreach (var directory in GetDirectories("./external/Xamarin.Android/lib/xbuild-frameworks/MonoAndroid/*"))
            {
                var name = directory.GetDirectoryName();
                if (!name.EndsWith("v1.0") && !name.EndsWith("v2.3") && !name.EndsWith("v8.0"))
                    DeleteDirectory(directory, true);
            }
        }
        finally
        {
            DeleteDirectory(tempDir, true);
            DeleteFile(temp);
        }
    });

Task("Generate-Android")
    .IsDependentOn("Download-Xamarin-Android")
    .IsDependentOn("Build-Binder")
    .IsDependentOn("Build-Android")
    .Does(() =>
    {
        var output = buildDir + Directory("android");
        Embeddinator($"-gen=Java -out={output} -platform=Android -compile {androidDll}");
    });

Task("Generate-Android-PCL")
    .IsDependentOn("Download-Xamarin-Android")
    .IsDependentOn("Build-Binder")
    .IsDependentOn("Build-PCL")
    .Does(() =>
    {
        var output = buildDir + Directory("pcl");
        Embeddinator($"-gen=Java -out={output} -platform=Android -compile {pclDll}");
    });

Task("Generate-Android-NetStandard")
    .IsDependentOn("Download-Xamarin-Android")
    .IsDependentOn("Build-Binder")
    .IsDependentOn("Build-NetStandard")
    .Does(() =>
    {
        var output = buildDir + Directory("netstandard");
        Embeddinator($"-gen=Java -out={output} -platform=Android -compile {netStandardDll}");
    });

Task("Generate-Android-FSharp")
    .IsDependentOn("Download-Xamarin-Android")
    .IsDependentOn("Build-Binder")
    .IsDependentOn("Build-FSharp-Android")
    .Does(() =>
    {
        var output = buildDir + Directory("fsharp");
        Embeddinator($"-gen=Java -out={output} -platform=Android -compile {fsharpAndroidDll}");
    });

void Gradle(string args)
{
    if (IsRunningOnWindows())
    {
        Exec("cmd.exe", "/c gradlew.bat --stacktrace " + args, "./tests/android");
    }
    else
    {
        Exec("./tests/android/gradlew", "--stacktrace " + args, "./tests/android");
    }
}

Task("Build-Android-Tests")
    .IsDependentOn("Generate-Android")
    .Does(() =>
    {
        CopyFile(buildDir + File("android/managed.aar"), File("./tests/android/managed/managed.aar"));
        CopyDirectory(Directory("./tests/common/java"), Directory("./tests/android/app/src/main/java"));
        Gradle("assemble");
    });

Task("Build-Android-PCL-Tests")
    .IsDependentOn("Generate-Android-PCL")
    .Does(() =>
    {
        CopyFile(buildDir + File("pcl/managed.aar"), File("./tests/android/managed/managed.aar"));
        CopyFiles("./tests/common/java/mono/embeddinator/*.java", Directory("./tests/android/app/src/main/java/mono/embeddinator"));
        Gradle("assemble");
    });

Task("Install-Android-Tests")
    .Does(() => Gradle("installDebug"));

Task("Run-Android-Tests")
    .IsDependentOn("Build-Android-Tests")
    .IsDependentOn("Install-Android-Tests")
    .Does(() => Gradle("connectedAndroidTest"));

Task("Run-Android-PCL-Tests")
    .IsDependentOn("Build-Android-PCL-Tests")
    .IsDependentOn("Install-Android-Tests")
    .Does(() => Gradle("-Pandroid.testInstrumentationRunnerArguments.class=mono.embeddinator.TestRunner connectedAndroidTest"));

Task("Android")
    .IsDependentOn("Run-Android-Tests");
