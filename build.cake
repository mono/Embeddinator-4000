#addin nuget:?package=Cake.DoInDirectory

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var buildDir = Directory("./build/lib") + Directory(configuration);
var embeddinator = File("MonoEmbeddinator4000.exe");
var managedDll = Directory("../../../tests/managed/generic/bin") + Directory(configuration) + File("managed.dll");
var javaHome = EnvironmentVariable("JAVA_HOME");

void Exec(string path, string args)
{
    Console.WriteLine(path + " " + args);

    var exitCode = StartProcess(path, args);
    if (exitCode != 0)
        throw new Exception(path + " failed!");
}

string GetJavaTool(string name)
{
    if (string.IsNullOrEmpty(javaHome))
        throw new Exception("Could not find JAVA_HOME!");

    return Directory(javaHome) + Directory("bin") + File(name);
}

string GetJavaClassPath()
{
    var classPath = new[]
    {
        "./external/junit/hamcrest-core-1.3.jar",
        "./external/junit/junit-4.12.jar",
        buildDir + Directory("java"),
        buildDir + File("java/managed.jar"),
    };

    return string.Join(";", classPath);
}

Task("Clean")
    .Does(() =>
    {
        CleanDirectory(buildDir);
    });

Task("Build-Binder")
    .Does(() =>
    {
        MSBuild("./build/projects/MonoEmbeddinator4000.csproj", settings => settings.SetConfiguration(configuration).SetVerbosity(Verbosity.Minimal));
    });

Task("Build-Managed")
    .Does(() =>
    {
        MSBuild("./tests/managed/generic/managed-generic.csproj", settings => settings.SetConfiguration(configuration).SetVerbosity(Verbosity.Minimal));
    });

Task("Generate-C")
    .IsDependentOn("Build-Binder")
    .IsDependentOn("Build-Managed")
    .Does(() =>
    {
        DoInDirectory(buildDir, () =>
        {
            Exec(embeddinator, $"-gen=c -out=c -platform=Windows -compile -target=shared {managedDll}");
        });
    });

Task("Generate-Java")
    .IsDependentOn("Generate-C")
    .Does(() =>
    {
        DoInDirectory(buildDir, () =>
        {
            Exec(embeddinator, $"-gen=Java -out=java -platform=Windows -compile -target=shared {managedDll}");
        });
    });

Task("Build-Java-Tests")
    .IsDependentOn("Generate-Java")
    .Does(() =>
    {
        var classPath = GetJavaClassPath();
        var output = buildDir + Directory("java");
        var tests = "./tests/common/java/mono/embeddinator/Tests.java";
        Exec(GetJavaTool("javac"), $"-cp {classPath} -d {output} -Xdiags:verbose -Xlint:deprecation {tests}");
    });

Task("Run-Java-Tests")
    .IsDependentOn("Build-Java-Tests")
    .Does(() =>
    {
        var classPath = GetJavaClassPath();
        Exec(GetJavaTool("java"), $"-cp {classPath} -D -Djna.dump_memory=true org.junit.runner.JUnitCore mono.embeddinator.Tests");
    });

Task("Default")
    .IsDependentOn("Generate-Java");

RunTarget(target);
