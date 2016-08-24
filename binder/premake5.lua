  project "MonoManagedToNative"
    SetupManagedProject()

    kind "ConsoleApp"
    language "C#"

    files { "../binder/**.cs" }

    libdirs { "../deps" }
  
    links
    {
      "System",
      "System.Core",
      "IKVM.Reflection",
      "CppSharp",
      "CppSharp.AST",
      "CppSharp.Generator"
    }