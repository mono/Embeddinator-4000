﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using CppSharp;
using CppSharp.Generators;
using Xamarin.Android.Tasks;
using Xamarin.Android.Tools;
using static MonoEmbeddinator4000.Helpers;

namespace MonoEmbeddinator4000
{
    public partial class Driver
    {
        private IEnumerable<string> GetOutputFiles(string pattern)
        {
            return Output.Files.Keys.Select(file => Path.Combine(Options.OutputDir, file))
                .Where(file => file.EndsWith(pattern, StringComparison.OrdinalIgnoreCase));
        }

        bool CompileCode()
        {
            IEnumerable<string> files = null;

            switch (Options.GeneratorKind)
            {
            case GeneratorKind.C:
                files = GetOutputFiles("c");
                break;
            case GeneratorKind.ObjectiveC:
                files = GetOutputFiles("c");
                files = files.Concat(GetOutputFiles("mm"));
                break;
            case GeneratorKind.CPlusPlus:
                files = GetOutputFiles("c");
                files = files.Concat(GetOutputFiles("cpp"));
                break;
            case GeneratorKind.Java:
                files = GetOutputFiles("java");
                break;
            }

            if (files == null || files.Count() == 0)
                throw new Exception("No generated files found.");

            if (Options.GeneratorKind != GeneratorKind.Java)
            {
                if (!CompileNativeCode(files))
                    return false;
            }

            if (Options.GeneratorKind == GeneratorKind.Java)
            {
                if (!CompileJava(files))
                    return false;

                if (!CreateJar())
                    return false;

                if (Options.Compilation.Platform == TargetPlatform.Android)
                {
                    if (!CreateAar())
                        return false;
                }
            }

            return true;
        }

        bool CompileJava(IEnumerable<string> files)
        {
            var executableSuffix = Platform.IsWindows ? ".exe" : string.Empty;
            var javaSdk = XamarinAndroid.JavaSdkPath;
            var javac = Path.Combine(javaSdk, "bin", "javac" + executableSuffix);
            var classesDir = Path.Combine(Options.OutputDir, "classes");
            var bootClassPath = Path.Combine(javaSdk, "jre", "lib", "rt.jar");

            if (Options.Compilation.Platform == TargetPlatform.Android)
            {
                bootClassPath = Path.Combine(XamarinAndroid.PlatformDirectory, "android.jar");
            }

            var javaFiles = files.Select(file => Path.GetFullPath(file)).ToList();

            var supportFiles = Directory.GetFiles(FindDirectory("support"), "*.java", SearchOption.AllDirectories)
                .Where(f => Options.Compilation.Platform == TargetPlatform.Android || Path.GetFileName(Path.GetDirectoryName(f)) != "android");
            javaFiles.AddRange(supportFiles);

            //NOTE: GenerateJavaStubs puts them in /src/
            if (Options.Compilation.Platform == TargetPlatform.Android)
            {
                var stubsPath = Path.Combine(Options.OutputDir, "src");
                if (Directory.Exists(stubsPath))
                {
                    var stubFiles = Directory.GetFiles(stubsPath, "*.java", SearchOption.AllDirectories);
                    javaFiles.AddRange(stubFiles);
                }
            }

            var args = new List<string> {
                string.Join(" ", javaFiles),
                $"-source {XamarinAndroid.JavaVersion} -target {XamarinAndroid.JavaVersion}",
                $"-bootclasspath \"{bootClassPath}\"",
                $"-d {classesDir}",
            };

            if (Options.Compilation.DebugMode)
                args.Add("-g");

            //Jar files needed: JNA, android.jar, mono.android.jar
            args.Add("-cp");

            var jnaJar = Path.Combine(FindDirectory("external"), "jna", "jna-4.4.0.jar");
            if (Options.Compilation.Platform == TargetPlatform.Android)
            {
                var androidJar = Path.Combine(XamarinAndroid.PlatformDirectory, "android.jar");
                var monoAndroidJar = XamarinAndroid.FindAssembly("mono.android.jar");
                var delimiter = Platform.IsWindows ? ";" : ":";
                var classpath = new List<string> { jnaJar, androidJar, monoAndroidJar };

                //Now we need to add any additional jars from binding projects to the classpath
                var intermediateDir = Path.Combine(Options.OutputDir, "obj");
                if (Directory.Exists(intermediateDir))
                {
                    foreach (var jar in Directory.GetFiles(intermediateDir, "*.jar", SearchOption.AllDirectories))
                    {
                        classpath.Add(jar);
                    }
                }

                //There are yet, another set of jar files
                string resourcePaths = Path.Combine(Options.OutputDir, XamarinAndroidBuild.IntermediateDir, XamarinAndroidBuild.ResourcePaths);
                if (File.Exists(resourcePaths))
                {
                    var document = new XmlDocument();
                    document.Load(resourcePaths);
                    foreach (XmlNode node in document.SelectNodes("/Paths/AdditionalJavaLibraryReferences/AdditionalJavaLibraryReference"))
                    {
                        classpath.Add(node.InnerText);
                    }
                }

                args.Add("\"" + string.Join(delimiter, classpath) + "\"");
            }
            else
            {
                args.Add(jnaJar);
            }
                
            //If "classes" directory doesn't exists, javac fails
            if (!Directory.Exists(classesDir))
                Directory.CreateDirectory(classesDir);

            var invocation = string.Join(" ", args);
            var output = Invoke(javac, invocation);

            return output.ExitCode == 0;
        }

        bool CreateJar()
        {
            var executableSuffix = Platform.IsWindows ? ".exe" : string.Empty;
            var jar = Path.Combine(XamarinAndroid.JavaSdkPath, "bin", "jar" + executableSuffix);
            var classesDir = Path.Combine(Options.OutputDir, "classes");
            var name = Path.GetFileNameWithoutExtension(Project.Assemblies[0]).Replace('-', '_');

            var args = new List<string> {
                "cf",
                Path.Combine(Options.OutputDir, name + ".jar"),
                $"-C {classesDir} ."
            };

            // On desktop Java, we need a few more files included
            if (Options.Compilation.Platform == TargetPlatform.MacOS)
            {
                //Copy native libs
                var platformDir = Path.Combine(classesDir, "darwin");
                if (!Directory.Exists(platformDir))
                    Directory.CreateDirectory(platformDir);

                var libName = $"lib{name}.dylib";
                var outputDir = Path.Combine(Options.OutputDir, libName);
                File.Copy(outputDir, Path.Combine(platformDir, libName), true);

                //Copy .NET assemblies
                var assembliesDir = Path.Combine(classesDir, "assemblies");
                if (!Directory.Exists(assembliesDir))
                    Directory.CreateDirectory(assembliesDir);

                foreach (var assembly in Project.Assemblies)
                {
                    File.Copy(assembly, Path.Combine(assembliesDir, Path.GetFileName(assembly)), true);
                }

                //Copy any referenced assemblies such as mscorlib.dll
                List<string> referencedAssemblies = new List<string>();
                foreach (var assembly in Assemblies)
                {
                    foreach (var reference in assembly.GetReferencedAssemblies())
                    {
                        if (!referencedAssemblies.Contains(reference.Name))
                            referencedAssemblies.Add(reference.Name);
                    }
                }

                var monoPath = ManagedToolchain.FindMonoPath();
                foreach (var reference in referencedAssemblies)
                {
                    var referencePath = Path.Combine(monoPath, "lib", "mono", "4.5", reference + ".dll");
                    if (File.Exists(referencePath))
                    {
                        File.Copy(referencePath, Path.Combine(assembliesDir, reference + ".dll"), true);
                    }
                }
            }

            //Embed JNA into our jar file
            using (var stream = File.OpenRead(Path.Combine(FindDirectory("external"), "jna", "jna-4.4.0.jar")))
            using (var zip = new ZipArchive(stream))
            {
                foreach (var entry in zip.Entries)
                {
                    //Skip META-INF
                    if (entry.FullName.StartsWith("META-INF", StringComparison.Ordinal))
                        continue;

                    var entryPath = Path.Combine(classesDir, entry.FullName);

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        if (!Directory.Exists(entryPath))
                            Directory.CreateDirectory(entryPath);
                    }
                    else
                    {
                        //NOTE: *.so files on Android will be packaged in a different way
                        if (Options.Compilation.Platform == TargetPlatform.Android && entry.Name.EndsWith(".so", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        using (var zipEntryStream = entry.Open())
                        using (var fileStream = File.Create(entryPath))
                        {
                            zipEntryStream.CopyTo(fileStream);
                        }
                    }
                }
            }

            //Embed mono.android.jar into our jar file
            if (Options.Compilation.Platform == TargetPlatform.Android)
            {
                using (var stream = File.OpenRead(XamarinAndroid.FindAssembly("mono.android.jar")))
                using (var zip = new ZipArchive(stream))
                {
                    foreach (var entry in zip.Entries)
                    {
                        //Skip META-INF
                        if (entry.FullName.StartsWith("META-INF", StringComparison.Ordinal))
                            continue;

                        var entryPath = Path.Combine(classesDir, entry.FullName);

                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            if (!Directory.Exists(entryPath))
                                Directory.CreateDirectory(entryPath);
                        }
                        else
                        {
                            using (var zipEntryStream = entry.Open())
                            using (var fileStream = File.Create(entryPath))
                            {
                                zipEntryStream.CopyTo(fileStream);
                            }
                        }
                    }
                }
            }

            var invocation = string.Join(" ", args);
            var output = Invoke(jar, invocation);
            return output.ExitCode == 0;
        }

        bool CreateAar()
        {
            var executableSuffix = Platform.IsWindows ? ".exe" : string.Empty;
            var jar = Path.Combine(XamarinAndroid.JavaSdkPath, "bin", "jar" + executableSuffix);
            var classesDir = Path.Combine(Options.OutputDir, "classes");
            var androidDir = Path.Combine(Options.OutputDir, "android");
            var name = Path.GetFileNameWithoutExtension(Project.Assemblies[0]).Replace('-', '_');

            var args = new List<string> {
                "cvf",
                Path.Combine(Options.OutputDir, name + ".aar"),
                $"-C {androidDir} ."
            };

            //Copy libmonosgen-2.0.so and libmonodroid.so
            const string libMonoSgen = "libmonosgen-2.0.so";
            const string libMonoAndroid = "libmono-android.release.so";

            foreach (var abi in Directory.GetDirectories(XamarinAndroid.LibraryPath))
            {
                var abiDir = Path.Combine(androidDir, "jni", Path.GetFileName(abi));

                string libMonoSgenSourcePath = Path.Combine(abi, libMonoSgen);
                string libMonoSgenDestPath = Path.Combine(abiDir, libMonoSgen);

                string libMonoAndroidSourcePath = Path.Combine(abi, libMonoAndroid);
                string libMonoAndroidDestPath = Path.Combine(abiDir, "libmonodroid.so"); //NOTE: Xamarin.Android runtime uses different name from APK

                if (!File.Exists(libMonoSgenSourcePath) || !File.Exists(libMonoAndroidSourcePath))
                    continue;

                if (!Directory.Exists(abiDir))
                    Directory.CreateDirectory(abiDir);
                File.Copy(libMonoSgenSourcePath, libMonoSgenDestPath, true);
                File.Copy(libMonoAndroidSourcePath, libMonoAndroidDestPath, true);
            }

            //Copy JNA native libs
            foreach (var file in Directory.GetFiles(Path.Combine(FindDirectory("external"), "jna"), "android-*"))
            {
                using (var stream = File.OpenRead(file))
                using (var zip = new ZipArchive(stream))
                {
                    foreach (var entry in zip.Entries)
                    {
                        //Skip non-*.so files
                        if (!entry.FullName.EndsWith(".so", StringComparison.Ordinal))
                            continue;

                        var arch = Path.GetFileNameWithoutExtension(file);
                        string abi;
                        switch (arch)
                        {
                        case "android-aarch64":
                            abi = "arm64-v8a";
                            break;
                        case "android-arm":
                            abi = "armeabi";
                            break;
                        case "android-armv7":
                            abi = "armeabi-v7a";
                            break;
                        case "android-x86-64":
                            abi = "x86_64";
                            break;
                        default:
                            abi = arch.Replace("android-", string.Empty);
                            break;
                        }

                        var abiDir = Path.Combine(androidDir, "jni", Path.GetFileName(abi));
                        if (!Directory.Exists(abiDir))
                            Directory.CreateDirectory(abiDir);

                        using (var zipEntryStream = entry.Open())
                        using (var fileStream = File.Create(Path.Combine(abiDir, entry.Name)))
                        {
                            zipEntryStream.CopyTo(fileStream);
                        }
                    }
                }
            }

            //Copy jar to android/classes.jar
            File.Copy(Path.Combine(Options.OutputDir, name + ".jar"), Path.Combine(androidDir, "classes.jar"), true);

            //Copy .NET assemblies
            var assembliesDir = Path.Combine(androidDir, "assets", "assemblies");
            if (!Directory.Exists(assembliesDir))
                Directory.CreateDirectory(assembliesDir);

            foreach (var assembly in Project.Assemblies)
            {
                File.Copy(assembly, Path.Combine(assembliesDir, Path.GetFileName(assembly)), true);
            }

            //Copy any referenced assemblies such as mscorlib.dll
            List<string> referencedAssemblies = new List<string>();
            foreach (var assembly in Assemblies)
            {
                foreach (var reference in assembly.GetReferencedAssemblies())
                {
                    if (!referencedAssemblies.Contains(reference.Name))
                        referencedAssemblies.Add(reference.Name);
                }
            }

            Diagnostics.Message("Linking assemblies...");

            //Performs Xamarin.Android build tasks such as Linking, Resource/Asset extraction, invoking aapt.
            var project = XamarinAndroidBuild.GeneratePackageProject(Assemblies, Options);
            if (!MSBuild(project))
                return false;

            //Generate Resource.designer.dll
            var resourceDesigner = new ResourceDesignerGenerator
            {
                Assemblies = Assemblies,
                MainAssembly = Assemblies[0].Location,
                OutputDirectory = assembliesDir,
                JavaResourceFile = Path.Combine(androidDir, "R.txt"),
            };
            resourceDesigner.Generate();

            if (!resourceDesigner.WriteAssembly())
            {
                //Let's generate CS if this failed
                string resourcePath = Path.Combine(Options.OutputDir, "Resource.designer.cs");
                resourceDesigner.WriteSource(resourcePath);
                throw new Exception($"Resource.designer.dll compilation failed! See {resourcePath} for details.");
            }

            //Runs some final processing on .NET assemblies
            XamarinAndroidBuild.ProcessAssemblies(Options.OutputDir);

            var invocation = string.Join(" ", args);
            var output = Invoke(jar, invocation);
            return output.ExitCode == 0;
        }

        bool MSBuild(string project)
        {
            var output = Invoke(XamarinAndroid.MSBuildPath, $"/nologo /verbosity:minimal {project}");
            return output.ExitCode == 0;
        }

        const string DLLExportDefine = "MONO_EMBEDDINATOR_DLL_EXPORT";

        bool CompileMSVC(IEnumerable<string> files)
        {
            List<ToolchainVersion> vsSdks;
            MSVCToolchain.GetVisualStudioSdks(out vsSdks);

            if (vsSdks.Count == 0)
                throw new Exception("Visual Studio SDK was not found on your system.");

            ToolchainVersion vsSdk;
            if (Options.Compilation.VsVersion == VisualStudioVersion.Latest)
                vsSdk = vsSdks.LastOrDefault();
            else
            {
                var exactVersion = vsSdks.Where(vs => (int)vs.Version == (int)Options.Compilation.VsVersion)
                    .Cast<ToolchainVersion?>().SingleOrDefault();
                if (!exactVersion.HasValue)
                    throw new Exception($"Visual Studio SDK version {Options.Compilation.VsVersion} was not found on your system.");

                vsSdk = exactVersion.Value;
            }

            var clBin = String.Empty;
            if ((int)vsSdk.Version == (int)VisualStudioVersion.VS2017)
            {
                var clFiles = System.IO.Directory.EnumerateFiles(Path.Combine(vsSdk.Directory, @"..\..\VC\Tools\MSVC"), "cl.exe", SearchOption.AllDirectories);
                clBin = clFiles.Where(s => s.Contains(@"x86\cl.exe")).First();
            }
            else
                clBin = Path.GetFullPath(Path.Combine(vsSdk.Directory, "..", "..", "VC", "bin", "cl.exe"));

            Diagnostics.Debug($"VS path {vsSdk.Directory}");

            var monoPath = ManagedToolchain.FindMonoPath();
            var outputPath = Path.Combine(Options.OutputDir, Options.LibraryName ??
                Path.GetFileNameWithoutExtension(Project.Assemblies[0]));

            var args = new List<string> {
                "/nologo",
                $"-D{DLLExportDefine}",
                $"-I\"{monoPath}\\include\\mono-2.0\"",
                string.Join(" ", files.Select(file => "\""+ Path.GetFullPath(file) + "\"")),
                $"\"{GetSgenLibPath(monoPath)}\"",
                Options.Compilation.CompileSharedLibrary ? "/LD" : string.Empty,
                $"/Fe{outputPath}"
            };

            var invocation = string.Join(" ", args);

            var vsVersion = (VisualStudioVersion)(int)vsSdk.Version;
            var includes = MSVCToolchain.GetSystemIncludes(vsVersion);

            var winSdks = new List<ToolchainVersion>();
            MSVCToolchain.GetWindowsKitsSdks(out winSdks);

            var libParentPath = Directory.GetParent(Directory.EnumerateDirectories(Path.Combine(winSdks.Last().Directory, "lib"), "um", SearchOption.AllDirectories).First());
            var libPaths = libParentPath.EnumerateDirectories();

            Dictionary<string, string> envVars = null;
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("INCLUDE")))
            {
                envVars = new Dictionary<string, string>();
                envVars["INCLUDE"] = string.Join(";", includes);

                var clLib = Path.GetFullPath(
                    Path.Combine(vsSdk.Directory, "..", "..", "VC", "lib"));
                envVars["LIB"] = clLib + ";" + string.Join(";", libPaths.Select(path => Path.Combine(path.FullName, "x86")));
            }

            var output = Invoke(clBin, invocation, envVars);
            return output.ExitCode == 0;
        }

        string GetSgenLibPath(string monoPath)
        {
            var libPath = Path.Combine(monoPath, "lib");
            var sgenPath = Path.Combine(libPath, "mono-2.0-sgen.lib");
            return File.Exists(sgenPath) ? sgenPath : Path.Combine(libPath, "monosgen-2.0.lib");
        }

        bool CompileClang(IEnumerable<string> files)
        {
            var xcodePath = XcodeToolchain.GetXcodeToolchainPath();
            var clangBin = Path.Combine(xcodePath, "usr/bin/clang");
            var monoPath = ManagedToolchain.FindMonoPath();

            var args = new List<string> {
                $"-D{DLLExportDefine}",
                "-framework CoreFoundation",
                $"-I\"{monoPath}/include/mono-2.0\"",
                $"-L\"{monoPath}/lib/\" -lmonosgen-2.0",
                string.Join(" ", files.ToList())
            };

            var sysroot = Path.Combine (XcodeToolchain.GetXcodeIncludesFolder (), "../..");
            args.Add ($"-isysroot {sysroot}");

            if (Options.Compilation.Target == CompilationTarget.SharedLibrary)
            {
                var name = Path.GetFileNameWithoutExtension(Project.Assemblies[0]);
                var libName = $"lib{name}.dylib";
                var outputPath = Path.Combine(Options.OutputDir, libName);
                args.Add($"-dynamiclib -install_name {libName} -o {outputPath}");
            }

            switch (Options.GeneratorKind)
            {
            case GeneratorKind.ObjectiveC:
                args.Add("-ObjC");
                args.Add("-lobjc");
                break;
            case GeneratorKind.CPlusPlus:
                args.Add("-x c++");
                break;
            }

            var invocation = string.Join(" ", args);
            var output = Invoke(clangBin, invocation);
            return output.ExitCode == 0;
        }

        bool CompileNDK(IEnumerable<string> files)
        {
            var monoPath = Path.Combine(ManagedToolchain.FindMonoPath(), "include", "mono-2.0");
            var name = Path.GetFileNameWithoutExtension(Project.Assemblies[0]);
            var libName = $"lib{name}.so";
            var ndkPath = AndroidSdk.AndroidNdkPath;

            foreach (var abi in new[] { "armeabi", "armeabi-v7a", "arm64-v8a", "x86", "x86_64" })
            {
                string extra = string.Empty;
                AndroidTargetArch targetArch;
                switch (abi)
                {
                    case "armeabi":
                        targetArch = AndroidTargetArch.Arm;
                        break;
                    case "armeabi-v7a":
                        targetArch = AndroidTargetArch.Arm;
                        extra = " -march=armv7-a -mfloat-abi=softfp -mfpu=vfpv3-d16";
                        break;
                    case "arm64-v8a":
                        targetArch = AndroidTargetArch.Arm64;
                        break;
                    case "x86":
                        targetArch = AndroidTargetArch.X86;
                        extra = " -march=i686 -mtune=intel -mssse3 -mfpmath=sse -m32";
                        break;
                    case "x86_64":
                        targetArch = AndroidTargetArch.X86_64;
                        extra = " -march=x86-64 -mtune=intel -msse4.2 -mpopcnt -m64";
                        break;
                    default:
                        throw new NotImplementedException();
                }

                var clangBin = NdkUtil.GetNdkClangBin(Path.Combine(ndkPath, "toolchains"), targetArch);
                var systemInclude = NdkUtil.GetNdkPlatformIncludePath(ndkPath, targetArch, XamarinAndroid.ApiLevel);
                var monoDroidPath = Path.Combine(XamarinAndroid.LibraryPath, abi);
                var abiDir = Path.Combine(Options.OutputDir, "android", "jni", abi);
                var outputPath = Path.Combine(abiDir, libName);

                if (!Directory.Exists(abiDir))
                    Directory.CreateDirectory(abiDir);

                var args = new List<string> {
                    $"--sysroot=\"{systemInclude}\"{extra}",
                    "-fdiagnostics-color",
                    $"-D{DLLExportDefine}",
                    $"-I\"{monoPath}\"",
                    $"-L\"{monoDroidPath}\" -lmonosgen-2.0 -lmono-android.release",
                    string.Join(" ", files.ToList()),
                    "--std=c99",
                    $"-shared -o {outputPath}",
                };

                var invocation = string.Join(" ", args);
                var output = Invoke(clangBin, invocation);
                if (output.ExitCode != 0)
                    return false;
            }
            return true;
        }

        bool CompileNativeCode(IEnumerable<string> files)
        {
            if (Platform.IsWindows)
            {
                if (Options.Compilation.Platform == TargetPlatform.Android)
                {
                    return CompileNDK(files);
                }
                else
                {
                    return CompileMSVC(files);
                }
            }
            else if (Platform.IsMacOS)
            {
                switch (Options.Compilation.Platform)
                {
                case TargetPlatform.iOS:
                case TargetPlatform.TVOS:
                case TargetPlatform.WatchOS:
                case TargetPlatform.Windows:
                    throw new NotSupportedException(
                        $"Cross compilation to target platform '{Options.Compilation.Platform}' is not supported.");
                case TargetPlatform.MacOS:
                    return CompileClang(files);
                case TargetPlatform.Android:
                    return CompileNDK(files);
                }
                return true;
            }

            throw new NotImplementedException();
        }
    }
}
