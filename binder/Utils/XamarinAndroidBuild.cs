using System;
using System.Collections.Generic;
using System.IO;
using CppSharp;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;

namespace MonoEmbeddinator4000
{
    static class XamarinAndroidBuild
    {
        public const string TargetFrameworkVersion = "v2.3";

        /// <summary>
        /// Generates a LinkAssemblies.proj file for MSBuild to invoke
        /// </summary>
        public static string GenerateLinkAssembliesProject(string xamarinPath, string mainAssembly, string outputDirectory, string assembliesDirectory)
        {
            mainAssembly = Path.GetFullPath(mainAssembly);
            assembliesDirectory = Path.GetFullPath(assembliesDirectory);

            var msBuildPath = Path.Combine(xamarinPath, "lib", "xbuild", "Xamarin", "Android");
            if (!msBuildPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.OrdinalIgnoreCase))
                msBuildPath = msBuildPath + Path.DirectorySeparatorChar;

            var solution = new ProjectCollection();
            var project = ProjectRootElement.Create();
            project.AddProperty("TargetFrameworkDirectory", string.Join(";", 
                Path.Combine(xamarinPath, "lib", "xbuild-frameworks", "MonoAndroid", "v1.0"),
                Path.Combine(xamarinPath, "lib", "xbuild-frameworks", "MonoAndroid", "v1.0", "Facades"),
                Path.Combine(xamarinPath, "lib", "xbuild-frameworks", "MonoAndroid", TargetFrameworkVersion)));
            project.AddImport(ProjectCollection.Escape(Path.Combine(msBuildPath, "Xamarin.Android.CSharp.targets")));

            //Build Target
            var target = project.AddTarget("Build");

            //ResolveAssemblies Task
            var resolveAssemblies = target.AddTask("ResolveAssemblies");
            resolveAssemblies.SetParameter("Assemblies", mainAssembly);
            resolveAssemblies.SetParameter("LinkMode", "$(AndroidLinkMode)");
            resolveAssemblies.SetParameter("ReferenceAssembliesDirectory", "$(TargetFrameworkDirectory)");
            resolveAssemblies.AddOutputItem("ResolvedAssemblies", "ResolvedAssemblies");
            resolveAssemblies.AddOutputItem("ResolvedUserAssemblies", "ResolvedUserAssemblies");
            resolveAssemblies.AddOutputItem("ResolvedFrameworkAssemblies", "ResolvedFrameworkAssemblies");

            //LinkAssemblies Task
            var linkAssemblies = target.AddTask("LinkAssemblies");
            linkAssemblies.SetParameter("UseSharedRuntime", "False");
            linkAssemblies.SetParameter("LinkMode", "$(AndroidLinkMode)");
            linkAssemblies.SetParameter("LinkSkip", "$(AndroidLinkSkip)");
            linkAssemblies.SetParameter("LinkDescriptions", "@(LinkDescription)");
            linkAssemblies.SetParameter("PortablePdbFiles", "@(PortablePdbFiles)");
            linkAssemblies.SetParameter("DumpDependencies", "True");
            linkAssemblies.SetParameter("ResolvedAssemblies", "@(ResolvedAssemblies)");
            linkAssemblies.SetParameter("MainAssembly", mainAssembly);
            linkAssemblies.SetParameter("OutputDirectory", assembliesDirectory);

            //NOTE: might remove this later
            var projectFile = Path.Combine(outputDirectory, "LinkAssemblies.proj");
            File.WriteAllText(projectFile, project.RawXml);
            return projectFile;
;        }
    }
}