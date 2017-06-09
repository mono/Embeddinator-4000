  project "MonoEmbeddinator4000"
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
      "IKVM.Reflection",
      "CppSharp",
      "CppSharp.AST",
      "CppSharp.Generator",
      "CppSharp.Parser",
      "CppSharp.Parser.CSharp",
      "Xamarin.Android.Tools",
      "Xamarin.MacDev"
    }
