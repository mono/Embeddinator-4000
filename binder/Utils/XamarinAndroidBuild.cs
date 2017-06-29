using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Xamarin.Android.Tools;
using CppSharp;

namespace MonoEmbeddinator4000
{
    static class XamarinAndroidBuild
    {
        public const string TargetFrameworkVersion = "v2.3";
        public const string MinSdkVersion = "9";
        public const string TargetSdkVersion = "25";
        const string LinkMode = "SdkOnly";

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

        static void ResolveAssemblies(ProjectTargetElement target, string xamarinPath, string mainAssembly)
        {
            //NOTE: [Export] requires Mono.Android.Export.dll
            string monoAndroidExport = Path.Combine(xamarinPath, "lib", "xbuild-frameworks", "MonoAndroid", TargetFrameworkVersion, "Mono.Android.Export.dll");

            var resolveAssemblies = target.AddTask("ResolveAssemblies");
            resolveAssemblies.SetParameter("Assemblies", mainAssembly + ";" + monoAndroidExport);
            resolveAssemblies.SetParameter("LinkMode", LinkMode);
            resolveAssemblies.SetParameter("ReferenceAssembliesDirectory", "$(TargetFrameworkDirectory)");
            resolveAssemblies.AddOutputItem("ResolvedAssemblies", "ResolvedAssemblies");
            resolveAssemblies.AddOutputItem("ResolvedUserAssemblies", "ResolvedUserAssemblies");
            resolveAssemblies.AddOutputItem("ResolvedFrameworkAssemblies", "ResolvedFrameworkAssemblies");
        }

        static void GenerateResourceDesigner(List<IKVM.Reflection.Assembly> assemblies, string xamarinPath, string mainAssembly, string outputDirectory, string packageName)
        {
            var unit = new CodeCompileUnit();
            unit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration("Android.Runtime.ResourceDesignerAttribute", 
                new CodeAttributeArgument(new CodeSnippetExpression("\"__embeddinator__.Resource\"")),
                new CodeAttributeArgument("IsApplication", new CodeSnippetExpression("true"))));

            var ns = new CodeNamespace("__embeddinator__");
            ns.Imports.Add(new CodeNamespaceImport("System"));
            ns.Imports.Add(new CodeNamespaceImport("Android.Runtime"));
            unit.Namespaces.Add(ns);

            var resource = new CodeTypeDeclaration("Resource")
            {
                Attributes = MemberAttributes.Public,
            };
            ns.Types.Add(resource);

            resource.CustomAttributes.Add(new CodeAttributeDeclaration("System.CodeDom.Compiler.GeneratedCodeAttribute",
                new CodeAttributeArgument(new CodeSnippetExpression("\"Xamarin.Android.Build.Tasks\"")),
                new CodeAttributeArgument(new CodeSnippetExpression("\"1.0.0.0\""))));

            var readField = new CodeMemberMethod
            {
                Name = "ReadField",
                Attributes = MemberAttributes.Private | MemberAttributes.Static,
                ReturnType = new CodeTypeReference("Int32"),
            };
            readField.Parameters.Add(new CodeParameterDeclarationExpression("IntPtr", "R"));
            readField.Parameters.Add(new CodeParameterDeclarationExpression("String", "fieldName"));
            resource.Members.Add(readField);

            readField.Statements.Add(new CodeAssignStatement(
                new CodeSnippetExpression("IntPtr fieldId"),
                new CodeSnippetExpression("JNIEnv.GetStaticFieldID(R, fieldName, \"I\")")));
            readField.Statements.Add(new CodeMethodReturnStatement(new CodeSnippetExpression("JNIEnv.GetStaticIntField(R, fieldId)")));

            var updateIdValues = new CodeMemberMethod
            {
                Name = "UpdateIdValues",
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
            };
            resource.Members.Add(updateIdValues);

            updateIdValues.Statements.Add(new CodeVariableDeclarationStatement("IntPtr", "R"));

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.DefinedTypes)
                {
                    if (type.Name == "Resource" && 
                        type.CustomAttributes.Any(a => 
                                                  a.AttributeType.FullName == "System.CodeDom.Compiler.GeneratedCodeAttribute" && 
                                                  a.ConstructorArguments.Count > 0 && 
                                                  a.ConstructorArguments[0].Value.ToString() == "Xamarin.Android.Build.Tasks"))
                    {
                        foreach (var nested in type.DeclaredNestedTypes)
                        {
                            if (nested.DeclaredFields.Any())
                            {
                                updateIdValues.Statements.Add(new CodeAssignStatement(
                                    new CodeSnippetExpression("R"),
                                    new CodeSnippetExpression($"JNIEnv.FindClass(\"{packageName}.R${nested.Name.ToLowerInvariant()}\");")));

                                foreach (var field in nested.DeclaredFields)
                                {
                                    var left = new CodeSnippetExpression(type.FullName + "." + nested.Name + "." + field.Name);
                                    var right = new CodeSnippetExpression("ReadField(R, \"" + field.Name + "\")");
                                    updateIdValues.Statements.Add(new CodeAssignStatement(left, right));
                                }
                            }
                        }
                    }
                }
            }

            var csc = new Microsoft.CSharp.CSharpCodeProvider();
            var parameters = new CompilerParameters
            {
                OutputAssembly = Path.Combine(outputDirectory, "android", "assets", "assemblies", "Resource.designer.dll"),
            };
            parameters.ReferencedAssemblies.Add(Path.Combine(xamarinPath, "lib", "xbuild-frameworks", "MonoAndroid", "v1.0", "System.dll"));
            parameters.ReferencedAssemblies.Add(Path.Combine(xamarinPath, "lib", "xbuild-frameworks", "MonoAndroid", TargetFrameworkVersion, "Mono.Android.dll"));
            parameters.ReferencedAssemblies.Add(mainAssembly);

            var results = csc.CompileAssemblyFromDom(parameters, unit);
            if (results.Errors.HasErrors)
            {
                foreach (var error in results.Errors)
                {
                    Diagnostics.Error("Error: {0}", error);
                }

                //Let's generate CS if this failed
                string resourcePath = Path.Combine(outputDirectory, "Resource.designer.cs");
                using (var stream = File.Create(resourcePath))
	            using (var writer = new StreamWriter(stream))
	            {
	                csc.GenerateCodeFromCompileUnit(unit, writer, new CodeGeneratorOptions());
	            }

                throw new Exception($"Resource.designer.dll compilation failed! See {resourcePath} for details.");
            }
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
        public static string GeneratePackageProject(List<IKVM.Reflection.Assembly> assemblies, string xamarinPath, string mainAssembly, string outputDirectory, string assembliesDirectory)
        {
            mainAssembly = Path.GetFullPath(mainAssembly);
            outputDirectory = Path.GetFullPath(outputDirectory);
            assembliesDirectory = Path.GetFullPath(assembliesDirectory);

            var intermediateDir = Path.Combine(outputDirectory, "obj");
            var androidDir = Path.Combine(outputDirectory, "android");
            var assetsDir = Path.Combine(androidDir, "assets");
            var resourceDir = Path.Combine(androidDir, "res");
            var manifestPath = Path.Combine(androidDir, "AndroidManifest.xml");
            var packageName = "com." + Path.GetFileNameWithoutExtension(mainAssembly).Replace('-', '_') + "_dll";
            var project = CreateProject(xamarinPath);
            var target = project.AddTarget("Build");

            //Generate Resource.designer.dll
            GenerateResourceDesigner(assemblies, xamarinPath, mainAssembly, outputDirectory, packageName);

            //ResolveAssemblies Task
            ResolveAssemblies(target, xamarinPath, mainAssembly);

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
            androidResources.AddItem("AndroidAsset", @"%(ResolvedAssetDirectories.Identity)\**\*");
            androidResources.AddItem("AndroidResource", @"%(ResolvedResourceDirectories.Identity)\**\*");

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
            aapt.SetParameter("ApiLevel", TargetSdkVersion);
            aapt.SetParameter("ExtraArgs", "--output-text-symbols " + androidDir);

            //NOTE: might avoid the temp file later
            var projectFile = Path.Combine(outputDirectory, "Package.proj");
            File.WriteAllText(projectFile, project.RawXml);
            return projectFile;
        }

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
<manifest xmlns:android=""http://schemas.android.com/apk/res/android"">
    <uses-sdk android:minSdkVersion=""{MinSdkVersion}"" android:targetSdkVersion=""{TargetSdkVersion}"" />
</manifest>");

            var project = CreateProject(xamarinPath);
            var target = project.AddTarget("Build");

            //ResolveAssemblies Task
            ResolveAssemblies(target, xamarinPath, mainAssembly);

            //GenerateJavaStubs Task
            var generateJavaStubs = target.AddTask("GenerateJavaStubs");
            generateJavaStubs.SetParameter("ResolvedAssemblies", "@(ResolvedAssemblies)");
            generateJavaStubs.SetParameter("ResolvedUserAssemblies", "@(ResolvedUserAssemblies)");
            generateJavaStubs.SetParameter("ManifestTemplate", manifestPath);
            generateJavaStubs.SetParameter("MergedAndroidManifestOutput", manifestPath);
            generateJavaStubs.SetParameter("AndroidSdkPlatform", TargetSdkVersion); //TODO: should be an option
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
            File.WriteAllText(projectFile, project.RawXml);
            return projectFile;
        }
    }
}