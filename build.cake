#!mono .cake/Cake/Cake.exe

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var buildDir = Directory("./build/lib") + Directory(configuration);
var embeddinator = buildDir + File("MonoEmbeddinator4000.exe");
var managedDll = Directory("./tests/managed/generic/bin") + Directory(configuration) + File("managed.dll");
var androidDll = Directory("./tests/managed/android/bin") + Directory(configuration) + File("managed.dll");
var pclDll = Directory("./tests/managed/pcl/bin") + Directory(configuration) + File("managed.dll");

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

void Exec(string path, string args = "", string workingDir = ".")
{
    var settings = new ProcessSettings
    {
        Arguments = args,
        WorkingDirectory = workingDir,
    };
    if (path.EndsWith(".exe") && !IsRunningOnWindows())
    {
        settings.Arguments = path + " " + args;
        path = "mono";
    }
    var exitCode = StartProcess(path, settings);
    if (exitCode != 0)
        throw new Exception(path + " failed!");
}

void Gradle(string args)
{
    if (IsRunningOnWindows())
    {
        Exec("cmd.exe", "/c gradlew.bat " + args, "./tests/android");
    }
    else
    {
        Exec("./tests/android/gradlew", args, "./tests/android");
    }
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

Task("Build-Android")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        MSBuild("./tests/managed/android/managed-android.csproj", settings => settings.SetConfiguration(configuration).SetVerbosity(Verbosity.Minimal));
    });

Task("Build-PCL")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        MSBuild("./tests/managed/pcl/managed-pcl.csproj", settings => settings.SetConfiguration(configuration).SetVerbosity(Verbosity.Minimal));
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

Task("Download-Xamarin-Android")
    .Does(() =>
    {
        var xamarinPath = Directory("./external/Xamarin.Android");
        if (!DirectoryExists(xamarinPath))
        {
            Console.WriteLine("Downloading Xamarin.Android SDK, this will take a while...");

            //We can also update this URL later from here: https://jenkins.mono-project.com/view/Xamarin.Android/job/xamarin-android/lastSuccessfulBuild/Azure/
            var artifact = "oss-xamarin.android_v7.4.99.16_Darwin-x86_64_master_e83c99c";
            var url = $"https://jenkins.mono-project.com/view/Xamarin.Android/job/xamarin-android/444/Azure/processDownloadRequest/xamarin-android/{artifact}.zip";
            var temp = DownloadFile(url);
            var tempDir = temp.GetDirectory() + "/" + artifact;
            try
            {
                Console.WriteLine("Unzipping Xamarin.Android SDK...");

                //Unzip into root of %TEMP%
                //Root directory of zip will contain {artifact} as directory name
                Unzip(temp, temp.GetDirectory());

                //Move bin/Release to final directory in ./external/Xamarin.Android
                MoveDirectory(Directory(tempDir) + Directory("./bin/Release"), xamarinPath);
            }
            finally
            {
                DeleteDirectory(tempDir, true);
                DeleteFile(temp);
            }
        }
        else
        {
            Console.WriteLine("Xamarin.Android SDK already downloaded...");
        }
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
    .IsDependentOn("Download-Xamarin-Android")
    .IsDependentOn("Build-Binder")
    .IsDependentOn("Build-Android")
    .Does(() =>
    {
        var output = buildDir + Directory("android");
        Exec(embeddinator, $"-gen=Java -out={output} -platform=Android -compile -target=shared {androidDll}");
    });

Task("Generate-Android-PCL")
    .IsDependentOn("Download-Xamarin-Android")
    .IsDependentOn("Build-Binder")
    .IsDependentOn("Build-PCL")
    .Does(() =>
    {
        var output = buildDir + Directory("pcl");
        Exec(embeddinator, $"-gen=Java -out={output} -platform=Android -compile -target=shared {pclDll}");
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

Task("Run-Java-Tests")
    .IsDependentOn("Build-Java-Tests")
    .Does(() =>
    {
        var java = Directory(javaHome) + File("bin/java");
        Exec(java, $"-cp {classPath} -Djna.dump_memory=true org.junit.runner.JUnitCore mono.embeddinator.Tests");
    });

Task("Run-Android-Tests")
    .IsDependentOn("Build-Android-Tests")
    .IsDependentOn("Install-Android-Tests")
    .Does(() => Gradle("connectedAndroidTest"));

Task("Run-Android-PCL-Tests")
    .IsDependentOn("Build-Android-PCL-Tests")
    .IsDependentOn("Install-Android-Tests")
    .Does(() => Gradle("-Pandroid.testInstrumentationRunnerArguments.class=mono.embeddinator.TestRunner connectedAndroidTest"));

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
    .IsDependentOn("Build-Binder");

//Runs all Android-related tests
Task("Android")
    .IsDependentOn("Run-Android-PCL-Tests")
    .IsDependentOn("Run-Android-Tests");

RunTarget(target);
