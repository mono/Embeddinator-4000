using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        void Invoke(string program, string arguments, Dictionary<string, string> envVars = null)
        {
            Diagnostics.Debug("Invoking: {0} {1}", program, arguments);

            var process = new Process();
            process.StartInfo.FileName = program;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            if (envVars != null)
                foreach (var kvp in envVars)
                    process.StartInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
            process.OutputDataReceived += (sender, args) => Diagnostics.Message("{0}", args.Data);
            Diagnostics.PushIndent();
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            Diagnostics.PopIndent();
        }

        private IEnumerable<string> GetOutputFiles(string pattern)
        {
            return Directory.EnumerateFiles(Options.OutputDir)
                    .Where(file => file.EndsWith(pattern, StringComparison.OrdinalIgnoreCase));
        }

        void AotAssemblies()
        {
            switch (Options.Platform)
            {
            case TargetPlatform.iOS:
            case TargetPlatform.TVOS:
            case TargetPlatform.WatchOS:
            {
                string aotCompiler = GetAppleAotCompiler(Options.Platform,
                    XamarinSdkRoot, is64bits: false);

                // Call the Mono AOT cross compiler for all input assemblies.
                foreach (var assembly in Options.Project.Assemblies)
                {
                    var args = GetAotArguments(App, assembly, Abi.ARMv7,
                        Path.GetFullPath(Options.OutputDir), assembly + ".o",
                        assembly + ".llvm.o", assembly + ".data");
                
                    Diagnostics.Debug("{0} {1}", aotCompiler, args);
                }
                break;
            }
            case TargetPlatform.Windows:
            case TargetPlatform.Android:
                throw new NotSupportedException(string.Format(
                    "AOT cross compilation to target platform '{0}' is not supported.",
                    Options.Platform));
            case TargetPlatform.MacOS:
                break;
            }
        }

        Xamarin.iOS.Tasks.DetectIPhoneSdks DetectIPhoneSdks
        {
            get
            {
                var detectAppleSdks = new Xamarin.iOS.Tasks.DetectIPhoneSdks {
                    TargetFrameworkIdentifier = GetXamarinTargetFrameworkName(Options.Platform)
                };
                
                if (!detectAppleSdks.Execute())
                    throw new Exception("Error detecting Xamarin.iOS SDK.");
                
                return detectAppleSdks;
            }
        }

        Xamarin.MacDev.MonoTouchSdk MonoTouchSdk
        {
            get
            {
                var monoTouchSdk = Xamarin.iOS.Tasks.IPhoneSdks.MonoTouch;
                if (monoTouchSdk.ExtendedVersion.Version.Major < 10)
                    throw new Exception("Unsupported Xamarin.iOS version, upgrade to 10 or newer.");
                
                return monoTouchSdk;
            }
        }

        Xamarin.MacDev.AppleSdk AppleSdk => DetectIPhoneSdks.CurrentSdk;

        string XamarinSdkRoot => DetectIPhoneSdks.XamarinSdkRoot;
        
        string OutputName => Path.GetFileNameWithoutExtension(Options.Project.Assemblies[0]);

        string GetOutputFolder()
        {
            var appName = $"{OutputName}.app";

            switch (Options.Platform)
            {
            case TargetPlatform.iOS:
            case TargetPlatform.TVOS:
            case TargetPlatform.WatchOS:
                var sdkName = App.Abi.IsSimulator() ? "Simulator" : string.Empty;
                return Path.Combine(Options.OutputDir, $"{Options.Platform}{sdkName}",
                    appName);
            case TargetPlatform.Windows:
            case TargetPlatform.Android:
            case TargetPlatform.MacOS:
                break;
            }

            return Path.Combine(Options.OutputDir, Options.Platform.ToString(),
                App.Abi.ToString(), appName);
        }

        void MTouch()
        {
            var sdk = AppleSdk.InstalledSdkVersions.First();

            var args = new List<string> {
                $"--embeddinator {Options.OutputDir}",
                $"--nostrip",
                $"--sdkroot {AppleSdk.DeveloperRoot}",
                $"--sdk {sdk}",
                $"--targetver {sdk}",
                $"--target-framework {Xamarin.Utils.TargetFramework.Xamarin_iOS_1_0}",
                $"--assembly-build-target=@all=framework={OutputName}.framework"
            };

            if (Options.DebugMode)
                args.Add("--debug");

            var targetArg = App.Abi.IsSimulator() ? "--sim" : "--dev";
            args.Add($"{targetArg} {GetOutputFolder()}");

            var xamarinAppleFramework = GetXamarinTargetFrameworkName(Options.Platform);
            var references = new List<string> {
                Path.Combine(MonoTouchSdk.LibDir, "mono", xamarinAppleFramework, $"{xamarinAppleFramework}.dll"),
                Path.Combine(MonoTouchSdk.LibDir, "mono", xamarinAppleFramework, "mscorlib.dll")
            };

            foreach (var @ref in references)
                args.Add($"--r {@ref}");

            foreach (var assembly in Options.Project.Assemblies)
                args.Add(assembly);
 
            var mtouchPath = Path.Combine(MonoTouchSdk.BinDir, "mtouch");
            var expandedArgs = string.Join(" ", args);

            Invoke(mtouchPath, expandedArgs);
        }

        void CompileCode()
        {
            IEnumerable<string> files = null;

            switch (Options.Language)
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

            if (Options.Language != GeneratorKind.Java)
                CompileNativeCode(files);

            if (Options.Language == GeneratorKind.Java)
                CompileJava(files);
        }

        bool initXamarinAndroidTools = false;

        void CompileJava(IEnumerable<string> files)
        {
            if (!initXamarinAndroidTools)
            {
                AndroidLogger.Info += AndroidLogger_Info;
                AndroidLogger.Error += AndroidLogger_Error;
                initXamarinAndroidTools = true;
            }

            AndroidSdk.Refresh();

            var executableSuffix = Platform.IsWindows ? ".exe" : string.Empty;
            var javac = $"{Path.Combine(AndroidSdk.JavaSdkPath, "bin", "javac" + executableSuffix)}";

            var args = new List<string> {
                string.Join(" ", files.Select(file => Path.GetFullPath(file)))
            };

            if (Options.DebugMode)
                args.Add("-g");

            var invocation = string.Join(" ", args);

            Invoke(javac, invocation);
        }

        private void AndroidLogger_Info(string task, string message)
        {
            Diagnostics.Debug(message);
        }

        private void AndroidLogger_Error(string task, string message)
        {
            Diagnostics.Error(message);
        }

        const string DLLExportDefine = "MONO_M2N_DLL_EXPORT";

        void CompileMSVC(IEnumerable<string> files)
        {
            List<ToolchainVersion> vsSdks;
            MSVCToolchain.GetVisualStudioSdks(out vsSdks);

            if (vsSdks.Count == 0)
                throw new Exception("Visual Studio SDK was not found on your system.");

            ToolchainVersion vsSdk;
            if (Options.VsVersion == VisualStudioVersion.Latest)
                vsSdk = vsSdks.LastOrDefault();
            else
            {
                var exactVersion = vsSdks.Where(vs => vs.Version == (float)Options.VsVersion).Cast<ToolchainVersion?>().SingleOrDefault();
                if (!exactVersion.HasValue)
                    throw new Exception($"Visual Studio SDK version {Options.VsVersion} was not found on your system.");

                vsSdk = exactVersion.Value;
            }
            
            var clBin = Path.GetFullPath(
                Path.Combine(vsSdk.Directory, "..", "..", "VC", "bin", "cl.exe"));

            var monoPath = ManagedToolchain.FindMonoPath();
            var output = Path.Combine(Options.OutputDir, Options.LibraryName ??
                Path.GetFileNameWithoutExtension(Options.Project.Assemblies[0]));

            var args = new List<string> {
                "/nologo",
                $"-D{DLLExportDefine}",
                $"-I\"{monoPath}\\include\\mono-2.0\"",
                string.Join(" ", files.Select(file => Path.GetFullPath(file))),
                $"\"{monoPath}\\lib\\monosgen-2.0.lib\"",
                Options.CompileSharedLibrary ? "/LD" : string.Empty,
                $"/Fe{output}"
            };

            var invocation = string.Join(" ", args);

            var vsVersion = (VisualStudioVersion)(int)vsSdk.Version;
            var includes = MSVCToolchain.GetSystemIncludes(vsVersion);

            Dictionary<string, string> envVars = null;
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("INCLUDE")))
            {
                envVars = new Dictionary<string, string>();
                envVars["INCLUDE"] = string.Join(";", includes);

                var clLib = Path.GetFullPath(
                    Path.Combine(vsSdk.Directory, "..", "..", "VC", "lib"));
                envVars["LIB"] = clLib;
            }

            Invoke(clBin, invocation, envVars);
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

            switch (Options.Language)
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
                switch (Options.Platform)
                {
                case TargetPlatform.iOS:
                case TargetPlatform.TVOS:
                case TargetPlatform.WatchOS:
                    MTouch();
                    break;
                case TargetPlatform.Windows:
                case TargetPlatform.Android:
                    throw new NotSupportedException(
                        $"Cross compilation to target platform '{Options.Platform}' is not supported.");
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
