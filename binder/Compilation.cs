﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using CppSharp;
using CppSharp.Generators;
using Xamarin.Android.Tools;
using Xamarin.Android.Tasks;

namespace MonoEmbeddinator4000
{
    public enum BitCodeMode
    {
        None = 0,
        ASMOnly = 1,
        LLVMOnly = 2,
        MarkerOnly = 3,
    }

    [Flags]
    public enum Abi
    {
        None   =   0,
        i386   =   1,
        ARMv6  =   2,
        ARMv7  =   4,
        ARMv7s =   8,
        ARM64 =   16,
        x86_64 =  32,
        Thumb  =  64,
        LLVM   = 128,
        ARMv7k = 256,
        SimulatorArchMask = i386 | x86_64,
        DeviceArchMask = ARMv6 | ARMv7 | ARMv7s | ARMv7k | ARM64,
        ArchMask = SimulatorArchMask | DeviceArchMask,
        Arch64Mask = x86_64 | ARM64,
        Arch32Mask = i386 | ARMv6 | ARMv7 | ARMv7s | ARMv7k,
    }

    public static class AbiExtensions 
    {
        public static string AsString (this Abi self)
        {
            var rv = (self & Abi.ArchMask).ToString ();
            if ((self & Abi.LLVM) == Abi.LLVM)
                rv += "+LLVM";
            if ((self & Abi.Thumb) == Abi.Thumb)
                rv += "+Thumb";
            return rv;
        }

        public static string AsArchString (this Abi self)
        {
            return (self & Abi.ArchMask).ToString ().ToLowerInvariant ();
        }

        public static bool IsSimulator(this Abi self)
        {
            return (self & Abi.SimulatorArchMask) != 0;
        }
    }

    public class Application
    {
        public Abi Abi;

        public bool EnableDebug;
        public bool PackageMdb;
        public bool EnableLLVMOnlyBitCode;
        public bool EnableMSym;

        public bool UseDlsym(string aname) { return false; }
    }

    public partial class Driver
    {
        public Application App { get; } = new Application();

        public static string GetXamarinTargetFrameworkName(TargetPlatform platform)
        {
            switch (platform) {
            case TargetPlatform.MacOS:
                return "Xamarin.Mac";
            case TargetPlatform.iOS:
                return "Xamarin.iOS";
            case TargetPlatform.WatchOS:
                return "Xamarin.WatchOS";
            case TargetPlatform.TVOS:
                return "Xamarin.TVOS";
            }

            throw new InvalidOperationException ("Unknown Apple target platform: " + platform);
        }

        public static string GetAppleAotCompiler(TargetPlatform platform, string cross_prefix, bool is64bits)
        {
            switch (platform) {
            case TargetPlatform.iOS:
                if (is64bits) {
                    return Path.Combine (cross_prefix, "bin", "arm64-darwin-mono-sgen");
                } else {
                    return Path.Combine (cross_prefix, "bin", "arm-darwin-mono-sgen");
                }
            case TargetPlatform.WatchOS:
                return Path.Combine (cross_prefix, "bin", "armv7k-unknown-darwin-mono-sgen");
            case TargetPlatform.TVOS:
                return Path.Combine (cross_prefix, "bin", "aarch64-unknown-darwin-mono-sgen");
            }

            throw new InvalidOperationException ("Unknown Apple target platform: " + platform);
        }

        public static string Quote (string f)
        {
            if (f.IndexOf (' ') == -1 && f.IndexOf ('\'') == -1 && f.IndexOf (',') == -1)
                return f;

            var s = new StringBuilder ();

            s.Append ('"');
            foreach (var c in f) {
                if (c == '"' || c == '\\')
                    s.Append ('\\');

                s.Append (c);
            }
            s.Append ('"');

            return s.ToString ();
        }

        public static string GetAotArguments (Application app, string filename, Abi abi,
            string outputDir, string outputFile, string llvmOutputFile, string dataFile)
        {
            string aot_args = string.Empty;
            string aot_other_args = string.Empty;
            bool debug_all = false;
            var debug_assemblies = new List<string>();

            string fname = Path.GetFileName (filename);
            var args = new StringBuilder ();
            bool enable_llvm = (abi & Abi.LLVM) != 0;
            bool enable_thumb = (abi & Abi.Thumb) != 0;
            bool enable_debug = app.EnableDebug;
            bool enable_mdb = app.PackageMdb;
            bool llvm_only = app.EnableLLVMOnlyBitCode;
            string arch = abi.AsArchString ();

            args.Append ("--debug ");

            if (enable_llvm)
                args.Append ("--llvm ");

            if (!llvm_only)
                args.Append ("-O=gsharedvt ");
            args.Append (aot_other_args).Append (" ");
            args.Append ("--aot=mtriple=");
            args.Append (enable_thumb ? arch.Replace ("arm", "thumb") : arch);
            args.Append ("-ios,");
            args.Append ("data-outfile=").Append (Quote (dataFile)).Append (",");
            args.Append (aot_args);
            if (llvm_only)
                args.Append ("llvmonly,");
            else
                args.Append ("full,");

            //var sdk_or_product = Profile.IsSdkAssembly (aname) || Profile.IsProductAssembly (aname);
            var sdk_or_product = false;

            if (enable_llvm)
                args.Append ("nodebug,");
            else if (!(enable_debug || enable_mdb))
                args.Append ("nodebug,");
            else if (debug_all || debug_assemblies.Contains (fname) || !sdk_or_product)
                args.Append ("soft-debug,");

            args.Append ("dwarfdebug,");

            /* Needed for #4587 */
            if (enable_debug && !enable_llvm)
                args.Append ("no-direct-calls,");

            if (!app.UseDlsym (filename))
                args.Append ("direct-pinvoke,");

            if (app.EnableMSym) {
                var msymdir = Quote (Path.Combine (outputDir, "Msym"));
                args.Append ($"msym-dir={msymdir},");
            }

            //if (enable_llvm)
                //args.Append ("llvm-path=").Append (MonoTouchDirectory).Append ("/LLVM/bin/,");

            if (!llvm_only)
                args.Append ("outfile=").Append (Quote (outputFile));
            if (!llvm_only && enable_llvm)
                args.Append (",");
            if (enable_llvm)
                args.Append ("llvm-outfile=").Append (Quote (llvmOutputFile));
            args.Append (" \"").Append (filename).Append ("\"");
            return args.ToString ();
        }

        /// <summary>
        /// Represents the output of a process invocation.
        /// </summary>
        public struct ProcessOutput
        {
            public int ExitCode;
            public string StandardOutput;
            public string StandardError;
        }

        ProcessOutput Invoke(string program, string arguments, Dictionary<string, string> envVars = null)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = program,
                    Arguments = arguments,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            if (envVars != null)
                foreach (var kvp in envVars)
                    process.StartInfo.EnvironmentVariables[kvp.Key] = kvp.Value;

            var standardOut = new StringBuilder();
            process.OutputDataReceived += (sender, args) => {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    standardOut.AppendLine(args.Data);
            };

            var standardError = new StringBuilder();
            process.ErrorDataReceived += (sender, args) => {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    standardError.AppendLine(args.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            var output = new ProcessOutput
            {
                ExitCode = process.ExitCode,
                StandardOutput = standardOut.ToString(),
                StandardError = standardError.ToString()
            };

            Diagnostics.Debug("Invoking: {0} {1}", program, arguments);
            Diagnostics.PushIndent();
            if (standardOut.Length > 0)
                Diagnostics.Message("{0}", standardOut.ToString());
            if (standardError.Length > 0)
                Diagnostics.Message("{0}", standardError.ToString());
            Diagnostics.PopIndent();

            return output;
        }

        private IEnumerable<string> GetOutputFiles(string pattern)
        {
            return Output.Files.Keys.Select(file => Path.Combine(Options.OutputDir, file))
                .Where(file => file.EndsWith(pattern, StringComparison.OrdinalIgnoreCase));
        }

        void AotAssemblies()
        {
            switch (Options.Compilation.Platform)
            {
            case TargetPlatform.iOS:
            case TargetPlatform.TVOS:
            case TargetPlatform.WatchOS:
            case TargetPlatform.Windows:
            case TargetPlatform.Android:
                throw new NotSupportedException(string.Format(
                    "AOT cross compilation to target platform '{0}' is not supported.",
                    Options.Compilation.Platform));
            case TargetPlatform.MacOS:
                break;
            }
        }

        string OutputName => Path.GetFileNameWithoutExtension(Project.Assemblies[0]);

        string GetOutputFolder()
        {
            var appName = $"{OutputName}.app";

            switch (Options.Compilation.Platform)
            {
            case TargetPlatform.iOS:
            case TargetPlatform.TVOS:
            case TargetPlatform.WatchOS:
                var sdkName = App.Abi.IsSimulator() ? "Simulator" : string.Empty;
                return Path.Combine(Options.OutputDir, $"{Options.Compilation.Platform}{sdkName}",
                    appName);
            case TargetPlatform.Windows:
            case TargetPlatform.Android:
            case TargetPlatform.MacOS:
                break;
            }

            return Path.Combine(Options.OutputDir, Options.Compilation.Platform.ToString(),
                App.Abi.ToString(), appName);
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

        bool initXamarinAndroidTools = false;

        string GetJavaSdkPath()
        {
            if (Options.Compilation.Platform == TargetPlatform.Android)
                return AndroidSdk.JavaSdkPath;

            if (Platform.IsWindows)
                return Environment.GetEnvironmentVariable("JAVA_HOME");

            // If we are running on macOS, invoke java_home to figure out Java path.
            if (Platform.IsMacOS)
                return Invoke("/usr/libexec/java_home", null, null).StandardOutput.Trim();

            return Environment.GetEnvironmentVariable("JAVA_HOME");
        }

        void RefreshAndroidSdk()
        {
            if (Options.Compilation.Platform == TargetPlatform.Android)
            {
                if (!initXamarinAndroidTools)
                {
                    AndroidLogger.Info += AndroidLogger_Info;
                    AndroidLogger.Error += AndroidLogger_Error;
                    initXamarinAndroidTools = true;
                }

                AndroidSdk.Refresh();
            }
        }

        bool CompileJava(IEnumerable<string> files)
        {
            RefreshAndroidSdk();

            var executableSuffix = Platform.IsWindows ? ".exe" : string.Empty;
            var javaSdk = GetJavaSdkPath();
            var javac = $"{Path.Combine(javaSdk, "bin", "javac" + executableSuffix)}";
            var classesDir = Path.Combine(Options.OutputDir, "classes");
            var bootClassPath = Path.Combine(javaSdk, "jre", "lib", "rt.jar");

            if (Options.Compilation.Platform == TargetPlatform.Android)
            {
                var maxVersion = AndroidSdk.GetInstalledPlatformVersions().Select(m => m.ApiLevel).Max();
                var androidDir = AndroidSdk.GetPlatformDirectory(maxVersion);
                bootClassPath = Path.Combine(androidDir, "android.jar");
            }

            var args = new List<string> {
                string.Join(" ", files.Select(file => Path.GetFullPath(file))),
                string.Join(" ", Directory.GetFiles(FindDirectory("support"), "*.java", SearchOption.AllDirectories)),
                "-source 1.7 -target 1.7",
                $"-bootclasspath {bootClassPath}",
                $"-d {classesDir}",
            };

            if (Options.Compilation.DebugMode)
                args.Add("-g");

            //JNA library
            args.Add("-cp");
            args.Add(Path.Combine(FindDirectory("external"), "jna", "jna-4.4.0.jar"));

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
            var jar = $"{Path.Combine(GetJavaSdkPath(), "bin", "jar" + executableSuffix)}";
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

            var invocation = string.Join(" ", args);
            var output = Invoke(jar, invocation);
            return output.ExitCode == 0;
        }

        bool CreateAar()
        {
            var executableSuffix = Platform.IsWindows ? ".exe" : string.Empty;
            var jar = $"{Path.Combine(GetJavaSdkPath(), "bin", "jar" + executableSuffix)}";
            var classesDir = Path.Combine(Options.OutputDir, "classes");
            var androidDir = Path.Combine(Options.OutputDir, "android");
            var name = Path.GetFileNameWithoutExtension(Project.Assemblies[0]).Replace('-', '_');

            var args = new List<string> {
                "cvf",
                Path.Combine(Options.OutputDir, name + ".aar"),
                $"-C {androidDir} ."
            };

            //Create an AndroidManifest.xml
            File.WriteAllText(Path.Combine(androidDir, "AndroidManifest.xml"),
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android""
    package=""com.{name}_dll""
    android:versionCode=""1""
    android:versionName=""1.0"" >

    <uses-sdk
        android:minSdkVersion=""9""
        android:targetSdkVersion=""25"" />

</manifest>");

            //Copy libmonosgen-2.0.so
            const string libMonoSgen = "libmonosgen-2.0.so";
            var monoDroidPath = Path.Combine(MonoDroidSdk.BinPath, "..", "lib", "xbuild", "Xamarin", "Android", "lib");
            foreach (var abi in Directory.GetDirectories(monoDroidPath))
            {
                var abiDir = Path.Combine(androidDir, "jni", Path.GetFileName(abi));
                var libDestPath = Path.Combine(abiDir, libMonoSgen);
                var libSourcePath = Path.Combine(abi, libMonoSgen);
                if (!File.Exists(libSourcePath))
                    continue;
                if (!Directory.Exists(abiDir))
                    Directory.CreateDirectory(abiDir);
                File.Copy(libSourcePath, libDestPath, true);
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

            foreach (var reference in referencedAssemblies)
            {
                var referencePath = Path.Combine(MonoDroidSdk.BinPath, "..", "lib", "mono", "2.1", reference + ".dll");
                if (File.Exists(referencePath))
                {
                    File.Copy(referencePath, Path.Combine(assembliesDir, reference + ".dll"), true);
                }
            }

            var invocation = string.Join(" ", args);
            var output = Invoke(jar, invocation);
            return output.ExitCode == 0;
        }

        string FindDirectory(string dir)
        {
            for (int i = 0; i <= 3; i++)
            {
                if (Directory.Exists(dir))
                    return Path.GetFullPath(dir);

                dir = Path.Combine("..", dir);
            }

            throw new Exception($"Cannot find {Path.GetFileName(dir)}!");
        }

        private void AndroidLogger_Info(string task, string message)
        {
            Diagnostics.Debug(message);
        }

        private void AndroidLogger_Error(string task, string message)
        {
            Diagnostics.Error(message);
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
            RefreshAndroidSdk();

            var monoPath = ManagedToolchain.FindMonoPath();
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
                var systemInclude = NdkUtil.GetNdkPlatformIncludePath(ndkPath, targetArch, 24); //NOTE: 24 should be an option?
                var monoDroidPath = Path.Combine(MonoDroidSdk.BinPath, "..", "lib", "xbuild", "Xamarin", "Android", "lib", abi);
                var abiDir = Path.Combine(Options.OutputDir, "android", "jni", abi);
                var outputPath = Path.Combine(abiDir, libName);

                if (!Directory.Exists(abiDir))
                    Directory.CreateDirectory(abiDir);

                var args = new List<string> {
                    $"--sysroot=\"{systemInclude}\"{extra}",
                    $"-D{DLLExportDefine}",
                    $"-I\"{monoPath}/include/mono-2.0\"",
                    $"-L\"{monoDroidPath}\" -lmonosgen-2.0",
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
