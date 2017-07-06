using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Xamarin.Android.Tools;

namespace MonoEmbeddinator4000
{
    /// <summary>
    /// Contains everything MSBuild-related for Xamarin.Android
    /// </summary>
    static class XamarinAndroidBuild
    {
        const string LinkMode = "SdkOnly";

        static ProjectRootElement CreateProject(string monoDroidPath)
        {
            var msBuildPath = Path.Combine(monoDroidPath, "lib", "xbuild", "Xamarin", "Android");
            if (!msBuildPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.OrdinalIgnoreCase))
                msBuildPath = msBuildPath + Path.DirectorySeparatorChar;

            var project = ProjectRootElement.Create();
            project.AddProperty("TargetFrameworkDirectory", string.Join(";", 
                Path.Combine(monoDroidPath, "lib", "xbuild-frameworks", "MonoAndroid", "v1.0"),
                Path.Combine(monoDroidPath, "lib", "xbuild-frameworks", "MonoAndroid", "v1.0", "Facades"),
                Path.Combine(monoDroidPath, "lib", "xbuild-frameworks", "MonoAndroid", XamarinAndroid.TargetFrameworkVersion)));
            project.AddImport(ProjectCollection.Escape(Path.Combine(msBuildPath, "Xamarin.Android.CSharp.targets")));

            return project;
        }

        static void ResolveAssemblies(ProjectTargetElement target, string monoDroidPath, string mainAssembly)
        {
            //NOTE: [Export] requires Mono.Android.Export.dll
            string monoAndroidExport = Path.Combine(monoDroidPath, "lib", "xbuild-frameworks", "MonoAndroid", XamarinAndroid.TargetFrameworkVersion, "Mono.Android.Export.dll");

            var resolveAssemblies = target.AddTask("ResolveAssemblies");
            resolveAssemblies.SetParameter("Assemblies", mainAssembly + ";" + monoAndroidExport);
            resolveAssemblies.SetParameter("LinkMode", LinkMode);
            resolveAssemblies.SetParameter("ReferenceAssembliesDirectory", "$(TargetFrameworkDirectory)");
            resolveAssemblies.AddOutputItem("ResolvedAssemblies", "ResolvedAssemblies");
            resolveAssemblies.AddOutputItem("ResolvedUserAssemblies", "ResolvedUserAssemblies");
            resolveAssemblies.AddOutputItem("ResolvedFrameworkAssemblies", "ResolvedFrameworkAssemblies");
        }


        /// <summary>
        /// Generates a Package.proj file for MSBuild to invoke
        /// - Generates Resource.designer.dll for rewiring resource values from the final Java project
        /// - Links .NET assemblies and places output into /android/assets/assemblies
        /// - Extracts assets and resources from Android library projects into /obj/
        /// - Copies assets and resources into AAR
        /// - Invokes aapt to generate R.txt
        /// - One day I would like to get rid of the temp files, but I could not get the MSBuild APIs to work in-process
        /// </summary>
        public static string GeneratePackageProject(List<IKVM.Reflection.Assembly> assemblies, string monoDroidPath, string outputDirectory, string assembliesDirectory)
        {
            var mainAssembly = assemblies[0].Location;
            outputDirectory = Path.GetFullPath(outputDirectory);
            assembliesDirectory = Path.GetFullPath(assembliesDirectory);

            var intermediateDir = Path.Combine(outputDirectory, "obj");
            var androidDir = Path.Combine(outputDirectory, "android");
            var assetsDir = Path.Combine(androidDir, "assets");
            var resourceDir = Path.Combine(androidDir, "res");
            var manifestPath = Path.Combine(androidDir, "AndroidManifest.xml");
            var packageName = "com." + Path.GetFileNameWithoutExtension(mainAssembly).Replace('-', '_') + "_dll";
            var project = CreateProject(monoDroidPath);
            var target = project.AddTarget("Build");

            //Generate Resource.designer.dll
            var resourceDesigner = new ResourceDesignerGenerator
            {
                Assemblies = assemblies,
                MonoDroidPath = monoDroidPath,
                MainAssembly = mainAssembly,
                OutputDirectory = outputDirectory,
                PackageName = packageName,
            };
            resourceDesigner.Generate();

            if (!resourceDesigner.WriteAssembly())
            {
                //Let's generate CS if this failed
                string resourcePath = resourceDesigner.WriteSource();
                throw new Exception($"Resource.designer.dll compilation failed! See {resourcePath} for details.");
            }

            //ResolveAssemblies Task
            ResolveAssemblies(target, monoDroidPath, mainAssembly);

            //LinkAssemblies Task
            var linkAssemblies = target.AddTask("LinkAssemblies");
            linkAssemblies.SetParameter("UseSharedRuntime", "False");
            linkAssemblies.SetParameter("LinkMode", LinkMode);
            linkAssemblies.SetParameter("LinkDescriptions", "@(LinkDescription)");
            linkAssemblies.SetParameter("DumpDependencies", "True");
            linkAssemblies.SetParameter("ResolvedAssemblies", "@(ResolvedAssemblies);" + Path.Combine(outputDirectory, "Resource.designer.dll"));
            linkAssemblies.SetParameter("MainAssembly", mainAssembly);
            linkAssemblies.SetParameter("OutputDirectory", assembliesDirectory);

            //ResolveLibraryProjectImports Task, extracts Android resources
            var resolveLibraryProject = target.AddTask("ResolveLibraryProjectImports");
            resolveLibraryProject.SetParameter("Assemblies", "@(ResolvedUserAssemblies)");
            resolveLibraryProject.SetParameter("UseShortFileNames", "False");
            resolveLibraryProject.SetParameter("ImportsDirectory", intermediateDir);
            resolveLibraryProject.SetParameter("OutputDirectory", intermediateDir);
            resolveLibraryProject.SetParameter("OutputImportDirectory", intermediateDir);
            resolveLibraryProject.AddOutputItem("ResolvedAssetDirectories", "ResolvedAssetDirectories");
            resolveLibraryProject.AddOutputItem("ResolvedResourceDirectories", "ResolvedResourceDirectories");

            //Create ItemGroup of Android files
            var androidResources = target.AddItemGroup();
            androidResources.AddItem("AndroidAsset", @"%(ResolvedAssetDirectories.Identity)\**\*").Condition = "'@(ResolvedAssetDirectories)' != ''";
            androidResources.AddItem("AndroidResource", @"%(ResolvedResourceDirectories.Identity)\**\*").Condition = "'@(ResolvedAssetDirectories)' != ''";

            //Copy Task, to copy AndroidAsset files
            var copy = target.AddTask("Copy");
            copy.SetParameter("SourceFiles", "@(AndroidAsset)");
            copy.SetParameter("DestinationFiles", $"@(AndroidAsset->'{assetsDir + Path.DirectorySeparatorChar}%(RecursiveDir)%(Filename)%(Extension)')");

            //Copy Task, to copy AndroidResource files
            copy = target.AddTask("Copy");
            copy.SetParameter("SourceFiles", "@(AndroidResource)");
            copy.SetParameter("DestinationFiles", $"@(AndroidResource->'{resourceDir + Path.DirectorySeparatorChar}%(RecursiveDir)%(Filename)%(Extension)')");

            //Aapt Task to generate R.txt
            var aapt = target.AddTask("Aapt");
            aapt.SetParameter("ImportsDirectory", outputDirectory);
            aapt.SetParameter("OutputImportDirectory", outputDirectory);
            aapt.SetParameter("ManifestFiles", manifestPath);
            aapt.SetParameter("ApplicationName", packageName);
            aapt.SetParameter("JavaPlatformJarPath", Path.Combine(AndroidSdk.GetPlatformDirectory(AndroidSdk.GetInstalledPlatformVersions().Select(v => v.ApiLevel).Max()), "android.jar"));
            aapt.SetParameter("JavaDesignerOutputDirectory", outputDirectory);
            aapt.SetParameter("AssetDirectory", assetsDir);
            aapt.SetParameter("ResourceDirectory", resourceDir);
            aapt.SetParameter("ToolPath", AndroidSdk.GetBuildToolsPaths().First());
            aapt.SetParameter("ToolExe", "aapt");
            aapt.SetParameter("ApiLevel", XamarinAndroid.TargetSdkVersion);
            aapt.SetParameter("ExtraArgs", "--output-text-symbols " + androidDir);

            //There is an extra /manifest/AndroidManifest.xml file created
            var removeDir = target.AddTask("RemoveDir");
            removeDir.SetParameter("Directories", Path.Combine(androidDir, "manifest"));

            //NOTE: might avoid the temp file later
            var projectFile = Path.Combine(outputDirectory, "Package.proj");
            project.Save(projectFile);
            return projectFile;
        }

        /// <summary>
        /// Generates AndroidManifest.xml
        /// </summary>
        public static void GenerateAndroidManifest(List<IKVM.Reflection.Assembly> assemblies, string path, bool includeProvider = true)
        {
            string name = assemblies[0].GetName().Name.Replace('-', '_');
            string provider = string.Empty;
            if (includeProvider)
            {
                provider = $"<provider android:name=\"mono.embeddinator.AndroidRuntimeProvider\" android:exported=\"false\" android:initOrder=\"{int.MaxValue}\" android:authorities=\"${{applicationId}}.mono.embeddinator.AndroidRuntimeProvider.__mono_init__\" />";
            }

            File.WriteAllText(path,
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android"" package=""com.{name}_dll"">
    <uses-sdk android:minSdkVersion=""{XamarinAndroid.MinSdkVersion}"" android:targetSdkVersion=""{XamarinAndroid.TargetSdkVersion}"" />
    <application>
        <meta-data android:name=""mono.embeddinator.mainassembly"" android:value=""{name}"" />
        {provider}
    </application>
</manifest>");
        }

        /// <summary>
        /// Generates a GenerateJavaStubs.proj file for MSBuild to invoke
        /// - Generates Java source code for each C# class that subclasses Java.Lang.Object
        /// - Generates AndroidManifest.xml
        /// - One day I would like to get rid of the temp files, but I could not get the MSBuild APIs to work in-process
        /// </summary>
        public static string GenerateJavaStubsProject(List<IKVM.Reflection.Assembly> assemblies, string monoDroidPath, string outputDirectory)
        {
            var mainAssembly = assemblies[0].Location;
            outputDirectory = Path.GetFullPath(outputDirectory);

            var androidDir = Path.Combine(outputDirectory, "android");
            var manifestPath = Path.Combine(androidDir, "AndroidManifest.xml");
            var packageName = "com." + Path.GetFileNameWithoutExtension(mainAssembly).Replace('-', '_') + "_dll";

            if (!Directory.Exists(androidDir))
                Directory.CreateDirectory(androidDir);

            //AndroidManifest.xml template
            GenerateAndroidManifest(assemblies, manifestPath, false);

            var project = CreateProject(monoDroidPath);
            var target = project.AddTarget("Build");

            //ResolveAssemblies Task
            ResolveAssemblies(target, monoDroidPath, mainAssembly);

            //GenerateJavaStubs Task
            var generateJavaStubs = target.AddTask("GenerateJavaStubs");
            generateJavaStubs.SetParameter("ResolvedAssemblies", "@(ResolvedAssemblies)");
            generateJavaStubs.SetParameter("ResolvedUserAssemblies", "@(ResolvedUserAssemblies)");
            generateJavaStubs.SetParameter("ManifestTemplate", manifestPath);
            generateJavaStubs.SetParameter("MergedAndroidManifestOutput", manifestPath);
            generateJavaStubs.SetParameter("AndroidSdkPlatform", XamarinAndroid.TargetSdkVersion); //TODO: should be an option
            generateJavaStubs.SetParameter("AndroidSdkDir", AndroidSdk.AndroidSdkPath);
            generateJavaStubs.SetParameter("OutputDirectory", outputDirectory);
            generateJavaStubs.SetParameter("ResourceDirectory", "$(MonoAndroidResDirIntermediate)");
            generateJavaStubs.SetParameter("AcwMapFile", "$(MonoAndroidIntermediate)acw-map.txt");

            //XmlPoke to fix up AndroidManifest
            var xmlPoke = target.AddTask("XmlPoke");
            xmlPoke.SetParameter("XmlInputPath", manifestPath);
            xmlPoke.SetParameter("Query", "/manifest/@package");
            xmlPoke.SetParameter("Value", packageName);

            //android:name
            xmlPoke = target.AddTask("XmlPoke");
            xmlPoke.SetParameter("XmlInputPath", manifestPath);
            xmlPoke.SetParameter("Namespaces", "<Namespace Prefix='android' Uri='http://schemas.android.com/apk/res/android' />");
            xmlPoke.SetParameter("Query", "/manifest/application/provider/@android:name");
            xmlPoke.SetParameter("Value", "mono.embeddinator.AndroidRuntimeProvider");

            //android:authorities
            xmlPoke = target.AddTask("XmlPoke");
            xmlPoke.SetParameter("XmlInputPath", manifestPath);
            xmlPoke.SetParameter("Namespaces", "<Namespace Prefix='android' Uri='http://schemas.android.com/apk/res/android' />");
            xmlPoke.SetParameter("Query", "/manifest/application/provider/@android:authorities");
            xmlPoke.SetParameter("Value", "${applicationId}.mono.embeddinator.AndroidRuntimeProvider.__mono_init__");

            //NOTE: might avoid the temp file later
            var projectFile = Path.Combine(outputDirectory, "GenerateJavaStubs.proj");
            project.Save(projectFile);
            return projectFile;
        }
    }
}