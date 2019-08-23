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
using static Embeddinator.Helpers;

namespace Embeddinator
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
            case GeneratorKind.Swift:
                files = GetOutputFiles("swift");
                break;
            }

            if (files == null || files.Count() == 0)
                throw new Exception("No generated files found.");

            if (Options.GeneratorKind != GeneratorKind.Java &&
                Options.GeneratorKind != GeneratorKind.Swift)
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

            if (Options.GeneratorKind == GeneratorKind.Swift)
            {
                if (!CompileSwift(files))
                    return false;
            }

            return true;
        }

        bool CompileSwift(IEnumerable<string> files)
        {
            if (!Platform.IsMacOS)
                throw new NotImplementedException();

            var xcodePath = XcodeToolchain.GetXcodeToolchainPath();
            var swiftcBin = Path.Combine(xcodePath, "usr/bin/swiftc");

            var moduleName = Path.GetFileNameWithoutExtension(Project.Assemblies[0]);

            var swiftVersion = 4;

            var sdkPath = Path.Combine(XcodeToolchain.GetXcodePath(),
                "Contents/Developer/Platforms/MacOSX.platform/Developer/SDKs");
            var sdk = Directory.EnumerateDirectories(sdkPath).First();

            bool compileSuccess = true;

            foreach (var file in files)
            {
                var args = new List<string>
                {
                    "-emit-module",
                    $"-emit-module-path {Options.OutputDir}",
                    $"-module-name {moduleName}",
                    $"-swift-version {swiftVersion}",
                    $"-sdk {sdk}",
                    $"-I \"{MonoSdkPath}/include/mono-2.0\"",
                };

                var bridgingHeader = Directory.EnumerateFiles(Options.OutputDir, "*.h")
                    .SingleOrDefault(header => Path.GetFileNameWithoutExtension(header) ==
                        Path.GetFileNameWithoutExtension(file));

                args.Add($"-import-objc-header {bridgingHeader}");

                args.Add(file);

                var invocation = string.Join(" ", args);
                var output = Invoke(swiftcBin, invocation);

                compileSuccess &= output.ExitCode == 0;
            }

            return compileSuccess;
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
                "-encoding UTF-8",
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

        string GetJnaPlatformDir()
        {
            // TODO: Use {os}-{arch} JNA format once we have better ABI support.
            switch (Options.Compilation.Platform)
            {
                case TargetPlatform.MacOS:
                    return "darwin";
                case TargetPlatform.Windows:
                    return "win32";
                case TargetPlatform.Linux:
                    return "linux";
            }

            throw new NotSupportedException();
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
            if (Options.Compilation.Platform != TargetPlatform.Android)
            {
                //Copy native libs
                var platformDir = Path.Combine(classesDir, GetJnaPlatformDir());
                if (!Directory.Exists(platformDir))
                    Directory.CreateDirectory(platformDir);

                var libName = (Options.Compilation.Platform == TargetPlatform.Windows) ?
                    $"{name}.dll" : $"lib{name}.dylib";
                var libFile = Path.Combine(Options.OutputDir, libName);
                if (File.Exists(libFile))
                {
                    var outputFile = Path.Combine(platformDir, libName);
                    File.Copy(libFile, outputFile, true);
                }

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

                foreach (var reference in referencedAssemblies)
                {
                    var referencePath = Path.Combine(MonoSdkPath, "lib", "mono", "4.5", reference + ".dll");
                    if (File.Exists(referencePath))
                    {
                        File.Copy(referencePath, Path.Combine(assembliesDir, reference + ".dll"), true);
                    }
                }

                // Copy the Mono runtime shared library to the JAR file
                var libDir = (Options.Compilation.Platform == TargetPlatform.Windows) ?
                    "bin" : "lib";

                switch(Options.Compilation.Platform)
                {
                    case TargetPlatform.Windows:
                        libName = "mono-2.0-sgen.dll";
                        break;
                    case TargetPlatform.MacOS:
                        libName = "libmonosgen-2.0.dylib";
                        break;
                    case TargetPlatform.Linux:
                        libName = "libmonosgen-2.0.so";
                        break;
                    default:
                        throw new NotImplementedException();
                }

                var monoLib = Path.Combine(MonoSdkPath, libDir, libName);

                if (!File.Exists(monoLib))
                    throw new Exception($"Cannot find Mono runtime shared library: {monoLib}");

                File.Copy(monoLib, Path.Combine(platformDir, libName), true);
            }

            //Embed JNA into our jar file
            //  If on Android, we do not need any native libraries
            var jnaJar = Path.Combine(FindDirectory("external"), "jna", "jna-4.4.0.jar");
            var filter = Options.Compilation.Platform == TargetPlatform.Android ?
                         entry => entry.Name.EndsWith(".class", StringComparison.Ordinal) :
                         default(Func<ZipArchiveEntry, bool>);
            XamarinAndroidBuild.ExtractJar(jnaJar, classesDir, filter);

            //Embed mono.android.jar into our jar file
            if (Options.Compilation.Platform == TargetPlatform.Android)
            {
                var monoAndroidJar = XamarinAndroid.FindAssembly("mono.android.jar");
                XamarinAndroidBuild.ExtractJar(monoAndroidJar, classesDir);

                //Look for other JAR file dependencies from the user's assemblies
                foreach(var assembly in Assemblies)
                {
                    var intermediateDir = Path.Combine(Options.OutputDir, XamarinAndroidBuild.IntermediateDir, Path.GetFileNameWithoutExtension(assembly.Location));
                    if (Directory.Exists(intermediateDir))
                    {
                        foreach (var dependency in Directory.GetFiles(intermediateDir, "*.jar", SearchOption.AllDirectories))
                        {
                            XamarinAndroidBuild.ExtractJar(dependency, classesDir);
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
            var vsSdks = MSVCToolchain.GetVisualStudioSdks();

            // Skip TestAgent VS instances as they do not provide native toolchains.
            vsSdks = vsSdks.Where(sdk => !sdk.Directory.Contains("TestAgent")).ToList();

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

            var clBin = string.Empty;
            var clLib = string.Empty;

            const string clArch = "x86";

            var isVS2017OrGreater = (int)vsSdk.Version >= (int)VisualStudioVersion.VS2017;
            if (isVS2017OrGreater)
            {
                var clBaseDir = Directory.EnumerateDirectories(Path.Combine(vsSdk.Directory, @"..\..\VC\Tools\MSVC")).Last();
                clBin = Path.Combine(clBaseDir, $"bin\\Hostx86\\{clArch}\\cl.exe");
                clLib = Path.Combine(clBaseDir, $"lib\\{clArch}");
            }
            else
            {
                clBin = Path.GetFullPath(Path.Combine(vsSdk.Directory, "..", "..", "VC", "bin", "cl.exe"));
                clLib = Path.GetFullPath(Path.Combine(vsSdk.Directory, "..", "..", "VC", "lib"));
            }

            Diagnostics.Debug($"VS path {vsSdk.Directory}");

            var outputPath = Path.Combine(Options.OutputDir, Options.LibraryName ??
                Path.GetFileNameWithoutExtension(Project.Assemblies[0]));

            var args = new List<string> {
                "/nologo",
                $"-D{DLLExportDefine}",
                $"-I\"{MonoSdkPath}\\include\\mono-2.0\"",
                string.Join(" ", files.Select(file => "\""+ Path.GetFullPath(file) + "\"")),
                $"\"{GetSgenLibPath(MonoSdkPath)}\"",
                Options.Compilation.CompileSharedLibrary ? "/LD" : string.Empty,
                $"/Fe{outputPath}"
            };

            var invocation = string.Join(" ", args);

            var vsVersion = (VisualStudioVersion)(int)vsSdk.Version;
            var includes = MSVCToolchain.GetSystemIncludes(vsVersion);

            var winSdks = MSVCToolchain.GetWindowsKitsSdks();

            var libParentPath = Directory.GetParent(Directory.EnumerateDirectories(
                Path.Combine(winSdks.Last().Directory, "lib"), "um", SearchOption.AllDirectories).First());
            var libPaths = libParentPath.EnumerateDirectories();

            Dictionary<string, string> envVars = null;
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("INCLUDE")))
            {
                envVars = new Dictionary<string, string>();
                envVars["INCLUDE"] = string.Join(";", includes);
                envVars["LIB"] = Path.GetFullPath(clLib) + ";" +
                    string.Join(";", libPaths.Select(path => Path.Combine(path.FullName, clArch)));
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

        bool CompileClangMac(IEnumerable<string> files)
        {
            var xcodePath = XcodeToolchain.GetXcodeToolchainPath();
            var clangBin = Path.Combine(xcodePath, "usr/bin/clang");

            var args = new List<string> {
                "-Wno-typedef-redefinition",
                $"-D{DLLExportDefine}",
                "-framework CoreFoundation",
                $"-I\"{MonoSdkPath}/include/mono-2.0\"",
                $"-L\"{MonoSdkPath}/lib/\" -lmonosgen-2.0",
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

        public static string FindInPath(string filename)
        {
            return new[] { Environment.CurrentDirectory }
                .Concat(Environment.GetEnvironmentVariable("PATH").Split(';', ':'))
                .Select(dir =>
                {
                    var path = Path.Combine(dir, filename);
                    Console.WriteLine(path);
                    return path;
                })
                .FirstOrDefault(File.Exists);
        }

        bool CompileClangLinux(IEnumerable<string> files)
        {
            var compilerBin = FindInPath("clang") ?? FindInPath("gcc");

            if (compilerBin == null)
                throw new Exception("Cannot find C++ compiler on the system.");

            var args = new List<string> {
                $"-std=gnu99 -D{DLLExportDefine}",
                $"-D_REENTRANT -I/usr/lib/pkgconfig/../../include/mono-2.0",
                $"-L/usr/lib/pkgconfig/../../lib -lmono-2.0 -lm -lrt -ldl -lpthread",
                string.Join(" ", files.ToList())
            };

            if (Options.Compilation.Target == CompilationTarget.SharedLibrary)
            {
                var name = Path.GetFileNameWithoutExtension(Project.Assemblies[0]);
                var libName = $"lib{name}.so";
                var outputPath = Path.Combine(Options.OutputDir, libName);
                args.Add($"-shared -fPIC -install_name {libName} -o {outputPath}");
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
            var output = Invoke(compilerBin, invocation);
            return output.ExitCode == 0;
        }

        bool CompileNDK(IEnumerable<string> files)
        {
            var monoPath = Path.Combine(MonoSdkPath, "include", "mono-2.0");
            var name = Path.GetFileNameWithoutExtension(Project.Assemblies[0]);
            var libName = $"lib{name}.so";
            var ndkPath = XamarinAndroid.AndroidSdk.AndroidNdkPath;

            foreach (var abi in new[] { "armeabi-v7a", "arm64-v8a", "x86", "x86_64" })
            {
                string extra = string.Empty;
                AndroidTargetArch targetArch;
                switch (abi)
                {
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

                bool isLLVM = false;
                var clangBin = NdkUtil.GetNdkClangBin(Path.Combine(ndkPath, "toolchains"), targetArch);
                if (string.IsNullOrEmpty(clangBin)) {
                    clangBin = NdkUtil.GetNdkClangBin(Path.Combine(ndkPath, "toolchains", "llvm"), targetArch);
                    isLLVM = true;
                }
                if (string.IsNullOrEmpty (clangBin))
                {
                    throw new Exception($"Unable to find NDK toolchain for {abi}!");
                }
                var systemInclude = NdkUtil.GetNdkPlatformIncludePath(ndkPath, targetArch, XamarinAndroid.ApiLevel, isLLVM);
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
                    "--std=c11",
                    "-fPIC",
                    $"-shared -o {outputPath}",
                };
                if (isLLVM)
                {
                    args.Add($"--target={NdkUtil.GetLlvmToolchainTarget(targetArch, XamarinAndroid.ApiLevel)}");
                }

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
                    return CompileClangMac(files);
                case TargetPlatform.Android:
                    return CompileNDK(files);
                }
                return true;
            }
            else
            {
                switch (Options.Compilation.Platform)
                {
                case TargetPlatform.Linux:
                    return CompileClangLinux(files);
                case TargetPlatform.Android:
                    return CompileNDK(files);
                }
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the Mono SDK install path.
        /// </summary>
        public static string MonoSdkPath
        {
            get { return monoSdkPath.Value; }
        }

        static Lazy<string> monoSdkPath = new Lazy<string>(() =>
        {
            string path = ManagedToolchain.FindMonoPath();

            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                throw new Exception("Cannot find Mono SDK, it needs to be installed on the system.");

            return path;
        });
    }
}
