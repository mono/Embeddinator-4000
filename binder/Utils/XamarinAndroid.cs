using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CppSharp;
using Xamarin.Android.Tools;
using static System.IO.Path;

namespace Embeddinator
{
    /// <summary>
    /// Contains various path logic for Xamarin.Android
    /// </summary>
    static class XamarinAndroid
    {
        public const string TargetFrameworkVersion = "v7.0";
        public const string MinSdkVersion = "9";
        public const int TargetSdkVersion = 24;
        public const string JavaVersion = "1.8";

        static readonly AndroidSdkInfo androidSdk;

        static XamarinAndroid ()
        {
            androidSdk = new AndroidSdkInfo (AndroidSdkLogger);
        }

        static void AndroidSdkLogger (TraceLevel level, string message)
        {
            switch (level)
            {
            case TraceLevel.Error:
                Diagnostics.Error (message);
                break;
            case TraceLevel.Warning:
                Diagnostics.Warning (message);
                break;
            default:
                Diagnostics.Debug (message);
                break;
            }
        }

        public static AndroidSdkInfo AndroidSdk => androidSdk;

        static string GetMonoDroidPath()
        {
            string external = Combine(Helpers.FindDirectory("external"), "Xamarin.Android");
            if (Directory.Exists(external))
                return external;

            string binPath = MonoDroidSdk.BinPath;

            //On Windows, it is generally correct, but probe for "lib"
            if (File.Exists(Combine(binPath, "lib")))
                return GetFullPath(MonoDroidSdk.BinPath);

            //On Mac, it is up one directory from BinPath
            return GetFullPath(Combine(MonoDroidSdk.BinPath, ".."));
        }

        static string GetMonoDroidLibPath()
        {
            var libPath = Combine(Path, "lib", "xbuild", "Xamarin", "Android", "lib");
            if (!Directory.Exists(libPath))
                libPath = Combine(Path, "lib");
            return libPath;
        }

        static Lazy<string> path = new Lazy<string>(GetMonoDroidPath);

        public static string Path
        {
            get { return path.Value; }
        }

        static Lazy<string> libraryPath = new Lazy<string>(GetMonoDroidLibPath);

        public static string LibraryPath
        {
            get { return libraryPath.Value; }
        }

        static Lazy<string[]> targetFrameworks = new Lazy<string[]>(() => new[]
        {
            Combine(Path, "lib", "xbuild-frameworks", "MonoAndroid", "v1.0"),
            Combine(Path, "lib", "xbuild-frameworks", "MonoAndroid", "v1.0", "Facades"),
            Combine(Path, "lib", "xbuild-frameworks", "MonoAndroid", TargetFrameworkVersion),
            Combine(Path, "lib", "xbuild-frameworks", "MonoAndroid", "v2.3"), //Mono.Android.Export.dll is here
        });

        public static string[] TargetFrameworkDirectories
        {
            get { return targetFrameworks.Value; }
        }

        /// <summary>
        /// Finds a Xamarin.Android assembly in v1.0, v1.0/Facades, or TargetFrameworkVersion
        /// NOTE: that this will also work for mono.android.jar
        /// </summary>
        public static string FindAssembly(string assemblyName)
        {
            foreach (var dir in TargetFrameworkDirectories)
            {
                string assemblyPath = Combine(dir, assemblyName);
                if (File.Exists(assemblyPath))
                    return assemblyPath;
            }

            throw new FileNotFoundException("Unable to find assembly!", assemblyName);
        }

        static Lazy<int> apiLevel = new Lazy<int> (() =>
        {
            for (int i = TargetSdkVersion; i > 0; i--)
            {
                if (androidSdk.IsPlatformInstalled (i))
                    return i;
            }

             throw new Exception ("Unable to find an installed API level!");
        });

        /// <summary>
        /// Right now we are choosing the max API level installed
        /// </summary>
        public static int ApiLevel
        {
            get { return apiLevel.Value; }
        }

        static Lazy<string> platformDirectory = new Lazy<string>(() => AndroidSdk.GetPlatformDirectory(ApiLevel));

        /// <summary>
        /// Gets the platform directory based on the max API level installed
        /// </summary>
        public static string PlatformDirectory
        {
            get { return platformDirectory.Value; }
        }

        static Lazy<string> javaSdkPath = new Lazy<string>(() =>
        {
            // If we are running on macOS, invoke java_home to figure out Java path.
            if (Platform.IsMacOS)
                return Helpers.Invoke("/usr/libexec/java_home", null, null).StandardOutput.Trim();
    
            string home = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrEmpty(home))
                return home;
    
            if (Platform.IsLinux)
            {
                // Only available on Debian-based distros, set JAVA_HOME for other distros.
                try
                {
                    var javaBin = Helpers.Invoke("update-alternatives", "--list java", null).StandardOutput.Trim();
                    if (!string.IsNullOrEmpty(javaBin))
                        return GetFullPath(Combine(GetDirectoryName(javaBin), "../.."));
                }
                finally { }
            }

            if (string.IsNullOrEmpty(home))
                throw new Exception("Cannot find Java SDK: JAVA_HOME environment variable is not set.");

            return string.Empty;
        });

        public static string JavaSdkPath
        {
            get { return javaSdkPath.Value; }
        }

        static Lazy<string> msBuildPath = new Lazy<string>(() =>
        {
            if (Platform.IsMacOS)
                return "/Library/Frameworks/Mono.framework/Versions/Current/Commands/msbuild";

            if (Platform.IsWindows)
            {
                List<ToolchainVersion> toolchains = MSVCToolchain.GetMSBuildSdks();
                if (!toolchains.Any())
                    return "MSBuild.exe";

                return Combine(toolchains.OrderByDescending(t => t.Version).Select(t => t.Directory).First(), "MSBuild.exe");
            }

            return "/usr/bin/msbuild";
        });

        public static string MSBuildPath
        {
            get { return msBuildPath.Value; }
        }
    }
}
