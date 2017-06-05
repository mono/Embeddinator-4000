using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xamarin.Android.Tools;

namespace Xamarin.Android.Tasks
{
    public static class NdkUtil
    {
        public static string GetNdkToolPrefix(string androidNdkPath, AndroidTargetArch arch)
        {
            var path = GetNdkTool(androidNdkPath, arch, "as");
            if (path != null)
                path = path.Substring(0, path.LastIndexOf("-") + 1);
            return path;
        }

        public static List<string> GetNdkToolchainPath(string androidNdkPath, AndroidTargetArch arch)
        {
            var toolchains = GetNdkToolchainDirectories(Path.Combine(androidNdkPath, "toolchains"), arch);
            if (!toolchains.Any())
                throw new Exception($"Toolchain directory for target {arch} was not found.");
            // Sort the toolchains paths in reverse so that we prefer the latest versions.
            Array.Sort(toolchains);
            Array.Reverse(toolchains);

            return new List<string>(toolchains);
        }

        public static string GetNdkTool(string androidNdkPath, AndroidTargetArch arch, string tool)
        {
            var toolchains = GetNdkToolchainPath(androidNdkPath, arch);
            string extension = OS.IsWindows ? ".exe" : string.Empty;
            List<string> toolPaths = null;
            foreach (var platbase in toolchains)
            {
                string path = Path.Combine(platbase, "prebuilt", AndroidSdk.AndroidNdkHostPlatform, "bin", GetNdkToolchainPrefix(arch) + tool + extension);
                if (File.Exists(path))
                    return path;
                if (toolPaths == null)
                    toolPaths = new List<string>();
                toolPaths.Add(path);
            }
            throw new Exception($"C compiler for target {arch} was not found. Tried paths: \"{string.Join("; ", toolPaths)}\"");
        }

        public static string GetNdkPlatformIncludePath(string androidNdkPath, AndroidTargetArch arch, int level)
        {
            string path = Path.Combine(androidNdkPath, "platforms", "android-" + level, "arch-" + GetPlatformArch(arch));
            if (!Directory.Exists(path))
                throw new InvalidOperationException(String.Format("Platform header files for target {0} and API Level {1} was not found. Expected path is \"{2}\"", arch, level, path));
            return path;
        }

        public static string GetNdkPlatformLibPath(string androidNdkPath, AndroidTargetArch arch, int level)
        {
            string path = Path.Combine(androidNdkPath, "platforms", "android-" + level, "arch-" + GetPlatformArch(arch), "usr", "lib");
            if (!Directory.Exists(path))
                throw new InvalidOperationException(String.Format("Platform library directory for target {0} and API Level {1} was not found. Expected path is \"{2}\"", arch, level, path));
            return path;
        }

        static string GetPlatformArch(AndroidTargetArch arch)
        {
            switch (arch)
            {
                case AndroidTargetArch.Arm:
                    return "arm";
                case AndroidTargetArch.Arm64:
                    return "arm64";
                case AndroidTargetArch.Mips:
                    return "mips";
                case AndroidTargetArch.X86:
                    return "x86";
                case AndroidTargetArch.X86_64:
                    return "x86_64";
            }
            return null;
        }

        static string[] GetNdkToolchainDirectories(string toolchainsPath, AndroidTargetArch arch)
        {
            if (!Directory.Exists(toolchainsPath))
                throw new Exception($"Missing Android NDK toolchains directory '{toolchainsPath}'. Please install the Android NDK.");
            switch (arch)
            {
                case AndroidTargetArch.Arm:
                    return Directory.GetDirectories(toolchainsPath, "arm-linux-androideabi-*");
                case AndroidTargetArch.Arm64:
                    return Directory.GetDirectories(toolchainsPath, "aarch64-linux-android-*");
                case AndroidTargetArch.X86:
                    return Directory.GetDirectories(toolchainsPath, "x86-*");
                case AndroidTargetArch.X86_64:
                    return Directory.GetDirectories(toolchainsPath, "x86_64-*");
                case AndroidTargetArch.Mips:
                    return Directory.GetDirectories(toolchainsPath, "mipsel-linux-android-*");
                default: // match any directory that contains the arch name.
                    return Directory.GetDirectories(toolchainsPath, "*" + arch + "*");
            }
        }

        static string GetNdkToolchainPrefix(AndroidTargetArch arch)
        {
            switch (arch)
            {
                case AndroidTargetArch.Arm:
                    return "arm-linux-androideabi-";
                case AndroidTargetArch.Arm64:
                    return "aarch64-linux-android-";
                case AndroidTargetArch.X86:
                    return "i686-linux-android-";
                case AndroidTargetArch.X86_64:
                    return "x86_64-linux-android-";
                case AndroidTargetArch.Mips:
                    return "mipsel-linux-android-";
                default:
                    // return empty. Since this method returns the "prefix", the resulting
                    // tool path just becomes the tool name i.e. "gcc" becomes "gcc".
                    // This should work for any custom arbitrary platform.
                    return String.Empty;
            }
        }

        public static string GetNdkClangBin(string toolchainsPath, AndroidTargetArch arch)
        {
            var toolChainDir = GetNdkToolchainDirectories(toolchainsPath, arch).First();
            var machineDir = Directory.GetDirectories(Path.Combine(toolChainDir, "prebuilt")).First();

            var gcc = Path.Combine(machineDir, "bin", GetNdkToolchainPrefix(arch) + "gcc");
            if (File.Exists(gcc))
                return gcc;

            throw new Exception($"Unable to find NDK toolchain for {arch}!");
        }

        static bool GetNdkToolchainRelease(string androidNdkPath, out string version)
        {
            var releaseVersionPath = Path.Combine(androidNdkPath, "RELEASE.txt");
            if (!File.Exists(releaseVersionPath))
            {
                version = string.Empty;
                return false;
            }

            version = File.ReadAllText(releaseVersionPath).Trim();
            return true;
        }

        public struct NdkVersion
        {
            public int Version;
            public string Revision;
        }

        public static bool GetNdkToolchainRelease(string androidNdkPath, out NdkVersion ndkVersion)
        {
            ndkVersion = new NdkVersion();

            string version;
            if (!GetNdkToolchainRelease(androidNdkPath, out version))
                return false;

            var match = Regex.Match(version, @"r(\d+)\s*(.*)\s+.*");
            if (!match.Success)
                return false;

            ndkVersion.Version = int.Parse(match.Groups[1].Value.Trim());
            ndkVersion.Revision = match.Groups[2].Value.Trim().ToLowerInvariant();

            return true;
        }

        public static bool IsNdk64BitArch(AndroidTargetArch arch)
        {
            return arch == AndroidTargetArch.Arm64 || arch == AndroidTargetArch.X86_64;
        }
    }
}