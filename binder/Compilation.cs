using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CppSharp;
using CppSharp.Generators;

namespace MonoEmbeddinator4000
{
    public partial class Driver
    {
        public static string GetAppleTargetFrameworkIdentifier(TargetPlatform platform)
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

        void InvokeCompiler(string compiler, string arguments, Dictionary<string, string> envVars = null)
        {
            Diagnostics.Debug("Invoking: {0} {1}", compiler, arguments);

            var process = new Process();
            process.StartInfo.FileName = compiler;
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

        void CompileCode()
        {
            var files = GetOutputFiles("c");

            switch (Options.Language)
            {
            case GeneratorKind.ObjectiveC:
                files = files.Concat(GetOutputFiles("mm"));
                break;
            case GeneratorKind.CPlusPlus:
                files = files.Concat(GetOutputFiles("cpp"));
                break;
            }

            const string exportDefine = "MONO_M2N_DLL_EXPORT";

            if (Platform.IsWindows)
            {
                List<ToolchainVersion> vsSdks;
                MSVCToolchain.GetVisualStudioSdks(out vsSdks);

                if (vsSdks.Count == 0)
                    throw new Exception("Visual Studio SDK was not found on your system.");

                var vsSdk = vsSdks.FirstOrDefault();
                var clBin = Path.GetFullPath(
                    Path.Combine(vsSdk.Directory, "..", "..", "VC", "bin", "cl.exe"));

                var monoPath = ManagedToolchain.FindMonoPath();
                var output = Options.LibraryName ??
                    Path.GetFileNameWithoutExtension(Options.Project.Assemblies[0]);
                output = Path.Combine(Options.OutputDir, output);
                var invocation = string.Format(
                    "/nologo /D{0} -I\"{1}\\include\\mono-2.0\" {2} \"{1}\\lib\\monosgen-2.0.lib\" {3} {4}",
                    exportDefine, monoPath, string.Join(" ", files.ToList()),
                    Options.CompileSharedLibrary ? "/LD" : string.Empty,
                    output);

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

                InvokeCompiler(clBin, invocation, envVars);

                return;
            }
            else if (Platform.IsMacOS)
            {
                switch (Options.Platform)
                {
                case TargetPlatform.iOS:
                case TargetPlatform.TVOS:
                case TargetPlatform.WatchOS:
                    var detectAppleSdks = new Xamarin.iOS.Tasks.DetectIPhoneSdks() {
                        TargetFrameworkIdentifier = GetAppleTargetFrameworkIdentifier(Options.Platform)
                    };

                    if (!detectAppleSdks.Execute())
                        throw new Exception("Error detecting Xamarin.iOS SDK.");

                    string aotCompiler = GetAppleAotCompiler(Options.Platform,
                        detectAppleSdks.XamarinSdkRoot, is64bits: false);

                    // TODO: Call the AOT cross compiler for all compiled assemblies.

                    break;
                case TargetPlatform.Windows:
                case TargetPlatform.Android:
                    throw new NotSupportedException(string.Format(
                        "Cross compilation to target platform '{0}' is not supported.",
                        Options.Platform));
                case TargetPlatform.MacOS:
                    var xcodePath = XcodeToolchain.GetXcodeToolchainPath();
                    var clangBin = Path.Combine(xcodePath, "usr/bin/clang");
                    var monoPath = ManagedToolchain.FindMonoPath();
    
                    var invocation = string.Format(
                        "-D{0} -framework CoreFoundation -I\"{1}/include/mono-2.0\" " +
                        "-L\"{1}/lib/\" -lmonosgen-2.0 {2}",
                        exportDefine, monoPath, string.Join(" ", files.ToList()));
    
                    InvokeCompiler(clangBin, invocation);       
                    break;
                }
                return;
            }

            throw new NotImplementedException();
        }
    }
}
