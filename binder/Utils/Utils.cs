using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MonoEmbeddinator4000
{
    public static class StringExtensions
    {
        public static string Repeat(this char chatToRepeat, int repeat)
        {
            return new string(chatToRepeat, repeat);
        }

        public static string Repeat(this string stringToRepeat, int repeat)
        {
            var builder = new StringBuilder(repeat * stringToRepeat.Length);
            for (var i = 0; i < repeat; i++)
            {
                builder.Append(stringToRepeat);
            }

            return builder.ToString();
        }

        public static IEnumerable<string> SplitAndKeep(this string s, string separator)
        {
            string[] obj = s.Split(new string[] { separator }, StringSplitOptions.None);

            for (int i = 0; i < obj.Length; i++)
            {
                string result = i == obj.Length - 1 ? obj[i] : obj[i] + separator;
                yield return result;
            }
        }

        public static string Replace(this string s, char[] separators, string newVal)
        {
            string[] temp;

            temp = s.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            return String.Join( newVal, temp );
        }
    }

    public static class Helpers
    {
        public static void CopyDirectory(string sourceDir, string targetDir)
        {
            var source = new DirectoryInfo(sourceDir);
            var target = new DirectoryInfo(targetDir);

            CopyAll(source, target);
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            // Check if the target directory exists, if not, create it.
            if (!Directory.Exists(target.FullName))
                Directory.CreateDirectory(target.FullName);

            // Copy each file into it's new directory.
            foreach (var file in source.GetFiles())
                file.CopyTo(Path.Combine(target.ToString(), file.Name), true);

            // Copy each subdirectory using recursion.
            foreach (var sourceSubDir in source.GetDirectories())
            {
                var nextTargetSubDir =
                    target.CreateSubdirectory(sourceSubDir.Name);
                CopyAll(sourceSubDir, nextTargetSubDir);
            }
        }
    }
}
