using System;
using NUnit.Framework;
using Xamarin.Android.Tools;
using System.IO;

namespace MonoEmbeddinator4000.Tests
{
    [TestFixture]
    public class XamarinAndroidTest
    {
        string cwd;

        [SetUp]
        public void SetUp()
        {
            cwd = Environment.CurrentDirectory;
            Environment.CurrentDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "..", ".."));

            AndroidLogger.Error += AndroidLogger_Error;
            AndroidLogger.Warning += AndroidLogger_Error;
            AndroidLogger.Info += AndroidLogger_Info;
        }

        [TearDown]
        public void TearDown()
        {
            Environment.CurrentDirectory = cwd;

            AndroidLogger.Error -= AndroidLogger_Error;
            AndroidLogger.Warning -= AndroidLogger_Error;
            AndroidLogger.Info -= AndroidLogger_Info;
        }

        void AndroidLogger_Error(string task, string message)
        {
            Assert.Fail("{0}: {1}", task, message);
        }

        void AndroidLogger_Info(string task, string message)
        {
            Console.WriteLine("{0}: {1}", task, message);
        }

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
    }
}
