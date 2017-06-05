﻿﻿﻿﻿﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using CppSharp;
using CppSharp.Generators;
using Xamarin.Android.Tools;

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
                    RedirectStandardOutput = true
                }
            };

            if (envVars != null)
                foreach (var kvp in envVars)
                    process.StartInfo.EnvironmentVariables[kvp.Key] = kvp.Value;

            var standardOut = new StringBuilder();
            process.OutputDataReceived += (sender, args) => standardOut.Append(args.Data);

            var standardError = new StringBuilder();
            process.ErrorDataReceived += (sender, args) => standardError.Append(args.Data);

            process.Start();
            process.BeginOutputReadLine();
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
                CompileNativeCode(files);

            if (Options.GeneratorKind == GeneratorKind.Java)
            {
                if (!CompileJava(files))
                    return false;

                CreateJar();
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
                return Invoke("/usr/libexec/java_home", null, null).StandardOutput;

            return Environment.GetEnvironmentVariable("JAVA_HOME");
        }

        bool CompileJava(IEnumerable<string> files)
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

            var executableSuffix = Platform.IsWindows ? ".exe" : string.Empty;
            var javac = $"{Path.Combine(GetJavaSdkPath(), "bin", "javac" + executableSuffix)}";
            var classesDir = Path.Combine(Options.OutputDir, "classes");

            var args = new List<string> {
                string.Join(" ", files.Select(file => Path.GetFullPath(file))),
                string.Join(" ", Directory.GetFiles(FindDirectory("support"), "*.java", SearchOption.AllDirectories)),
                "-d",
                classesDir
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

        void CreateJar()
        {
            var executableSuffix = Platform.IsWindows ? ".exe" : string.Empty;
            var jar = $"{Path.Combine(GetJavaSdkPath(), "bin", "jar" + executableSuffix)}";
            var classesDir = Path.Combine(Options.OutputDir, "classes");
            var name = Path.GetFileNameWithoutExtension(Project.Assemblies[0]);

            var args = new List<string> {
                "cvf",
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
                var output = Path.Combine(Options.OutputDir, libName);
                File.Copy(output, Path.Combine(platformDir, libName), true);

                //Copy .NET assemblies
                var assembliesDir = Path.Combine(classesDir, "assemblies");
                if (!Directory.Exists(assembliesDir))
                    Directory.CreateDirectory(assembliesDir);

                foreach (var assembly in Project.Assemblies)
                {
                    File.Copy(assembly, Path.Combine(assembliesDir, Path.GetFileName(assembly)), true);
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
                        using (var zipEntryStream = entry.Open())
                        using (var fileStream = File.Create(entryPath))
                        {
                            zipEntryStream.CopyTo(fileStream);
                        }
                    }
                }
            }

            var invocation = string.Join(" ", args);
            Invoke(jar, invocation);
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

        void CompileMSVC(IEnumerable<string> files)
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
            var output = Path.Combine(Options.OutputDir, Options.LibraryName ??
                Path.GetFileNameWithoutExtension(Project.Assemblies[0]));

            var args = new List<string> {
                "/nologo",
                $"-D{DLLExportDefine}",
                $"-I\"{monoPath}\\include\\mono-2.0\"",
                string.Join(" ", files.Select(file => "\""+ Path.GetFullPath(file) + "\"")),
                $"\"{GetSgenLibPath(monoPath)}\"",
                Options.Compilation.CompileSharedLibrary ? "/LD" : string.Empty,
                $"/Fe{output}"
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

            Invoke(clBin, invocation, envVars);
        }

        string GetSgenLibPath(string monoPath)
        {
            var libPath = Path.Combine(monoPath, "lib");
            var sgenPath = Path.Combine(libPath, "mono-2.0-sgen.lib");
            return File.Exists(sgenPath) ? sgenPath : Path.Combine(libPath, "monosgen-2.0.lib");
        }

        void CompileClang(IEnumerable<string> files)
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
                var output = Path.Combine(Options.OutputDir, libName);
                args.Add($"-dynamiclib -install_name {libName} -o {output}");
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

            Invoke(clangBin, invocation);
        }

        void CompileNativeCode(IEnumerable<string> files)
        {
            if (Platform.IsWindows)
            {
                CompileMSVC(files);
                return;
            }
            else if (Platform.IsMacOS)
            {
                switch (Options.Compilation.Platform)
                {
                case TargetPlatform.iOS:
                case TargetPlatform.TVOS:
                case TargetPlatform.WatchOS:
                case TargetPlatform.Windows:
                case TargetPlatform.Android:
                    throw new NotSupportedException(
                        $"Cross compilation to target platform '{Options.Compilation.Platform}' is not supported.");
                case TargetPlatform.MacOS:
                    CompileClang(files);
                    break;
                }
                return;
            }

            throw new NotImplementedException();
        }
    }
}
