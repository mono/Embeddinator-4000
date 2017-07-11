using System;
using System.IO;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace MonoEmbeddinator4000.Tests
{
    /// <summary>
    /// This is a very simple implementation of ApprovalTests: http://approvaltests.com/
    /// - I would prefer to use ApprovalTests, but had issues running on Mono
    /// </summary>
    public static class Approvals
    {
        static readonly string basePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(Approvals).Assembly.Location), "..", "..", "Approvals"));

        static string GetPath(string sourceFile, string member, string suffix)
        {
            string fileName = string.Join(".", Path.GetFileNameWithoutExtension(sourceFile), member, suffix, "txt");
            return Path.Combine(basePath, fileName);
        }

        /// <summary>
        /// Verifies a body of text against the approved text on disk.
        /// - Make sure to call this method directly from the unit test method, so that [CallerFilePath] works properly
        /// </summary>
        public static void Verify(string text, [CallerFilePath]string sourceFile = null, [CallerMemberName]string member = null)
        {
            string received = GetPath(sourceFile, member, "received");
            string approved = GetPath(sourceFile, member, "approved");
            File.WriteAllText(received, text);
            FileAssert.AreEqual(approved, received);
            File.Delete(received);
        }

        /// <summary>
        /// Do not commit code using this method. It should be used temporarily to approve a test.
        /// </summary>
        [Obsolete("Do not commit code using this method. It should be used temporarily to approve a test.")]
        public static void Approve(string text, [CallerFilePath]string sourceFile = null, [CallerMemberName]string member = null)
        {
#if !DEBUG
            //Fail release builds on purpose
            Assert.Fail("This test is using Approvals.Approve() when it should be using Approvals.Verify()!");
#endif
            string approved = GetPath(sourceFile, member, "approved");
            File.WriteAllText(approved, text);
            Verify(text, sourceFile, member);
        }
    }
}
