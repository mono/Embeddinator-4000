﻿using System;
using NUnit.Framework;
using Xamarin.Android.Tools;
using System.IO;

namespace Embeddinator.Tests
{
    [TestFixture]
    public class XamarinAndroidTest : CurrentDirectoryTest
    {
        [Test]
        public void PathExists()
        {
            DirectoryAssert.Exists(XamarinAndroid.Path);
        }

        [Test]
        public void LibraryPathExists()
        {
            DirectoryAssert.Exists(XamarinAndroid.LibraryPath);
        }

        [Test]
        public void JavaSdkPathExists()
        {
            DirectoryAssert.Exists(XamarinAndroid.JavaSdkPath);
        }

        [Test]
        public void TargetFrameworkDirectories()
        {
            foreach (var dir in XamarinAndroid.TargetFrameworkDirectories)
            {
                DirectoryAssert.Exists(dir);
            }
        }

        [Test]
        public void System()
        {
            string file = XamarinAndroid.FindAssembly("System.dll");
            FileAssert.Exists(file);
        }

        [Test]
        public void SystemRuntime()
        {
            string file = XamarinAndroid.FindAssembly("System.Runtime.dll");
            FileAssert.Exists(file);
        }

        [Test]
        public void JavaInterop()
        {
            string file = XamarinAndroid.FindAssembly("Java.Interop.dll");
            FileAssert.Exists(file);
        }

        [Test]
        public void MonoAndroid()
        {
            string file = XamarinAndroid.FindAssembly("Mono.Android.dll");
            FileAssert.Exists(file);
        }

        [Test]
        public void MonoAndroidJar()
        {
            string file = XamarinAndroid.FindAssembly("mono.android.jar");
            FileAssert.Exists(file);
        }

        [Test]
        public void PlatformDirectory()
        {
            string dir = XamarinAndroid.PlatformDirectory;
            DirectoryAssert.Exists(dir);
        }

        [Test]
        public void AndroidJar()
        {
            string file = Path.Combine(XamarinAndroid.PlatformDirectory, "android.jar");
            FileAssert.Exists(file);
        }

        [Test]
        public void MSBuild()
        {
            string msbuild = XamarinAndroid.MSBuildPath;
            var output = Helpers.Invoke(msbuild, "/version");
            Assert.AreEqual(0, output.ExitCode);
        }
    }
}
