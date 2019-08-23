bool IsRunningOnMacOS()
{
    if (Environment.OSVersion.Platform != PlatformID.Unix)
        return false;

    return System.IO.File.Exists("/System/Library/CoreServices/SystemVersion.plist") ||
           System.IO.File.Exists("/System/Library/CoreServices/ServerVersion.plist");
}

bool IsRunningOnLinux()
{
    return IsRunningOnUnix() && !IsRunningOnMacOS();
}

void Exec(string path, ProcessSettings settings)
{
    if (path.EndsWith(".exe") && !IsRunningOnWindows())
    {
        settings.Arguments.Prepend("--debug " + path + " ");
        path = "mono";
    }

    Verbose($"Executing: {path} {settings.Arguments.Render()}");

    var exitCode = StartProcess(path, settings);
    if (exitCode != 0)
        throw new Exception(path + " failed!");
}

void Exec(string path, string args = "", string workingDir = ".")
{
    var settings = new ProcessSettings
    {
        Arguments = args,
        WorkingDirectory = workingDir,
    };
    Exec(path, settings);
}

string CaptureProcessOutput(string path, string args = "")
{
    using (var process = StartAndReturnProcess(path,
        new ProcessSettings { Arguments = args, RedirectStandardOutput = true }))
    {
        process.WaitForExit();
        return process.GetStandardOutput().First().Trim();
    }
}

void Embeddinator(string args)
{
    var embeddinator = buildDir + File("Embeddinator-4000.exe");
    Exec(embeddinator, "-verbose " + args);
}

void Premake(string file, string args, string action)
{
    var premakePath = Directory("./external/CppSharp/build/") + (IsRunningOnWindows() ?
        File("premake5.exe") : IsRunningOnMacOS() ? File("premake5-osx") : File("premake5-linux-64"));
    Exec(premakePath, $"--file={file} {args} {action}");
}

MSBuildSettings MSBuildSettings()
{
    var settings = new MSBuildSettings { Configuration = configuration };

    if (IsRunningOnWindows())
    {
        // Find MSBuild for Visual Studio 2019 and newer
        DirectoryPath vsLatest = VSWhereLatest();
        FilePath msBuildPath = vsLatest?.CombineWithFilePath("./MSBuild/Current/Bin/MSBuild.exe");

        // Find MSBuild for Visual Studio 2017
        if (msBuildPath != null && !FileExists(msBuildPath))
            msBuildPath = vsLatest.CombineWithFilePath("./MSBuild/15.0/Bin/MSBuild.exe");

        // Have we found MSBuild yet?
        if (!FileExists(msBuildPath))
        {
            throw new Exception($"Failed to find MSBuild: {msBuildPath}");
        }

        Information("Building using MSBuild at " + msBuildPath);
        settings.ToolPath = msBuildPath;

        var java_home = Environment.GetEnvironmentVariable("JAVA_HOME_8_X64");
        if (!string.IsNullOrEmpty(java_home))
        {
            Information("JAVA_HOME_8_X64 set: " + java_home);
            settings = settings.WithProperty("JavaSdkDirectory", java_home);
        }
    }
    else
    {
        settings.ToolPath = Context.Tools.Resolve("msbuild");
    }

    return settings;
}
