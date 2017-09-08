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

void Premake(string file, string args, string action)
{
    var premakePath = Directory("./external/CppSharp/build/") + (IsRunningOnWindows() ?
        File("premake5.exe") : IsRunningOnMacOS() ? File("premake5-osx") : File("premake5-linux-64"));
    Exec(premakePath, $"--file={file} {args} {action}");
}
