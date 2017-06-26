using System;
using System.IO;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Xamarin.Android.Tools;

namespace MonoEmbeddinator4000
{
    static class XamarinAndroidBuild
    {
        public const string TargetFrameworkVersion = "v2.3";

        static ProjectRootElement CreateProject(string xamarinPath)
        {
            var msBuildPath = Path.Combine(xamarinPath, "lib", "xbuild", "Xamarin", "Android");
            if (!msBuildPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.OrdinalIgnoreCase))
                msBuildPath = msBuildPath + Path.DirectorySeparatorChar;

            var project = ProjectRootElement.Create();
            project.AddProperty("TargetFrameworkDirectory", string.Join(";", 
                Path.Combine(xamarinPath, "lib", "xbuild-frameworks", "MonoAndroid", "v1.0"),
                Path.Combine(xamarinPath, "lib", "xbuild-frameworks", "MonoAndroid", "v1.0", "Facades"),
                Path.Combine(xamarinPath, "lib", "xbuild-frameworks", "MonoAndroid", TargetFrameworkVersion)));
            project.AddImport(ProjectCollection.Escape(Path.Combine(msBuildPath, "Xamarin.Android.CSharp.targets")));

            return project;
        }

        /// <summary>
        /// Generates a LinkAssemblies.proj file for MSBuild to invoke
        /// - Links .NET assemblies and places output into /android/assets/assemblies
        /// - One day I would like to get rid of the temp files, but I could not get the MSBuild APIs to work in-process
        /// </summary>
        public static string GenerateLinkAssembliesProject(string xamarinPath, string mainAssembly, string outputDirectory, string assembliesDirectory)
        {
            mainAssembly = Path.GetFullPath(mainAssembly);
            assembliesDirectory = Path.GetFullPath(assembliesDirectory);

            var project = CreateProject(xamarinPath);
            var target = project.AddTarget("Build");

            //ResolveAssemblies Task
            var resolveAssemblies = target.AddTask("ResolveAssemblies");
            resolveAssemblies.SetParameter("Assemblies", mainAssembly);
            resolveAssemblies.SetParameter("LinkMode", "$(AndroidLinkMode)");
            resolveAssemblies.SetParameter("ReferenceAssembliesDirectory", "$(TargetFrameworkDirectory)");
            resolveAssemblies.AddOutputItem("ResolvedAssemblies", "ResolvedAssemblies");
            resolveAssemblies.AddOutputItem("ResolvedUserAssemblies", "ResolvedUserAssemblies");
            resolveAssemblies.AddOutputItem("ResolvedFrameworkAssemblies", "ResolvedFrameworkAssemblies");

            //LinkAssemblies Task
            var linkAssemblies = target.AddTask("LinkAssemblies");
            linkAssemblies.SetParameter("UseSharedRuntime", "False");
            linkAssemblies.SetParameter("LinkMode", "$(AndroidLinkMode)");
            linkAssemblies.SetParameter("LinkSkip", "$(AndroidLinkSkip)");
            linkAssemblies.SetParameter("LinkDescriptions", "@(LinkDescription)");
            linkAssemblies.SetParameter("DumpDependencies", "True");
            linkAssemblies.SetParameter("ResolvedAssemblies", "@(ResolvedAssemblies)");
            linkAssemblies.SetParameter("MainAssembly", mainAssembly);
            linkAssemblies.SetParameter("OutputDirectory", assembliesDirectory);

            //NOTE: might avoid the temp file later
            var projectFile = Path.Combine(outputDirectory, "LinkAssemblies.proj");
            File.WriteAllText(projectFile, project.RawXml);
            return projectFile;
;        }

        /// <summary>
        /// Generates a GenerateJavaStubs.proj file for MSBuild to invoke
        /// - Generates Java source code for each C# class that subclasses Java.Lang.Object
        /// - Generates AndroidManifest.xml
        /// - One day I would like to get rid of the temp files, but I could not get the MSBuild APIs to work in-process
        /// </summary>
        public static string GenerateJavaStubsProject(string xamarinPath, string mainAssembly, string outputDirectory)
        {
            mainAssembly = Path.GetFullPath(mainAssembly);
            outputDirectory = Path.GetFullPath(outputDirectory);

            var androidDir = Path.Combine(outputDirectory, "android");
            var manifestPath = Path.Combine(androidDir, "AndroidManifest.xml");
            var packageName = "com." + Path.GetFileNameWithoutExtension(mainAssembly).Replace('-', '_') + "_dll";

            if (!Directory.Exists(androidDir))
                Directory.CreateDirectory(androidDir);

            //AndroidManifest.xml templatel
            File.WriteAllText(manifestPath,
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android""
    package=""{packageName}""
    android:versionCode=""1""
    android:versionName=""1.0"" >

    <uses-sdk
        android:minSdkVersion=""9""
        android:targetSdkVersion=""25"" />
    <application>
        <provider
            android:name=""mono.embeddinator.AndroidRuntimeProvider""
            android:exported=""false""
            android:initOrder=""{int.MaxValue}""
            android:authorities=""${{applicationId}}.mono.embeddinator.AndroidRuntimeProvider.__mono_init__"" />
    </application>
</manifest>");

            var project = CreateProject(xamarinPath);
            var target = project.AddTarget("Build");

            //ResolveAssemblies Task
            var resolveAssemblies = target.AddTask("ResolveAssemblies");
            resolveAssemblies.SetParameter("Assemblies", mainAssembly);
            resolveAssemblies.SetParameter("LinkMode", "$(AndroidLinkMode)");
            resolveAssemblies.SetParameter("ReferenceAssembliesDirectory", "$(TargetFrameworkDirectory)");
            resolveAssemblies.AddOutputItem("ResolvedAssemblies", "ResolvedAssemblies");
            resolveAssemblies.AddOutputItem("ResolvedUserAssemblies", "ResolvedUserAssemblies");
            resolveAssemblies.AddOutputItem("ResolvedFrameworkAssemblies", "ResolvedFrameworkAssemblies");

            //GenerateJavaStubs Task
            var generateJavaStubs = target.AddTask("GenerateJavaStubs");
            generateJavaStubs.SetParameter("ResolvedAssemblies", "@(ResolvedAssemblies)");
            generateJavaStubs.SetParameter("ResolvedUserAssemblies", "@(ResolvedUserAssemblies)");
            generateJavaStubs.SetParameter("ManifestTemplate", manifestPath);
            generateJavaStubs.SetParameter("MergedManifestDocuments", "@(ExtractedManifestDocuments)");
            generateJavaStubs.SetParameter("Debug", "False");
            generateJavaStubs.SetParameter("NeedsInternet", "$(AndroidNeedsInternetPermission)");
            generateJavaStubs.SetParameter("AndroidSdkPlatform", "25"); //TODO: should be an option
            generateJavaStubs.SetParameter("AndroidSdkDir", AndroidSdk.AndroidSdkPath);
            generateJavaStubs.SetParameter("PackageName", packageName);
            generateJavaStubs.SetParameter("ManifestPlaceholders", "$(AndroidManifestPlaceholders)");
            generateJavaStubs.SetParameter("OutputDirectory", outputDirectory);
            generateJavaStubs.SetParameter("MergedAndroidManifestOutput", manifestPath + ".merged");
            generateJavaStubs.SetParameter("UseSharedRuntime", "False");
            generateJavaStubs.SetParameter("EmbedAssemblies", "False");
            generateJavaStubs.SetParameter("ResourceDirectory", "$(MonoAndroidResDirIntermediate)");
            generateJavaStubs.SetParameter("PackageNamingPolicy", "$(AndroidPackageNamingPolicy)");
            generateJavaStubs.SetParameter("AcwMapFile", "$(MonoAndroidIntermediate)acw-map.txt");

            //NOTE: might avoid the temp file later
            var projectFile = Path.Combine(outputDirectory, "GenerateJavaStubs.proj");
            File.WriteAllText(projectFile, project.RawXml);
            return projectFile;
        }
    }
}