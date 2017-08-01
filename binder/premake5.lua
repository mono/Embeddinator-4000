  project "Embeddinator-4000"
    SetupManagedProject()

    kind "ConsoleApp"
    language "C#"

    files { "../binder/**.cs" }

    libdirs { "../deps" }
  
    links
    {
      "System",
      "System.Core",
      "System.IO.Compression",
      "System.Xml",
      "IKVM.Reflection",
      "CppSharp",
      "CppSharp.AST",
      "CppSharp.Generator",
      "CppSharp.Parser",
      "CppSharp.Parser.CSharp",
      "Xamarin.Android.Tools",
      "Xamarin.MacDev",
      "Microsoft.Build.Engine",
      "Microsoft.Build",
      "Microsoft.Build.Framework"
    }
