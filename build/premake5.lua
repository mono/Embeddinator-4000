-- This is the starting point of the build scripts for the project.
-- It defines the common build settings that all the projects share
-- and calls the build scripts of all the sub-projects.

dofile "Helpers.lua"
dofile "Tests.lua"

solution "native-binder"

  configurations { "Debug", "Release" }
  flags { "Unicode", "Symbols" }
  
  location (builddir)
  objdir (path.join(builddir, "obj"))
  targetdir (libdir)
  libdirs { libdir }

  startproject "native-binder"
  
  configuration "Release"
    flags { "Optimize" }

  configuration {}

  project "native-binder"
    SetupManagedProject()

    kind "ConsoleApp"
    language "C#"

    location "../binder"
    files { "../binder/**.cs" }

    libdirs { "../deps" }
  
    links
    {
      "System",
      "System.Core",
      "IKVM.Reflection",
      "CppSharp.AST"
    }

  --include("../CppSharp/src/Core")
  include("../CppSharp/src/AST")

  --[[
  external "IKVM.Reflection"
    location ("../ikvm/reflect")
    uuid "4CB170EF-DFE6-4A56-9E1B-A85449E827A7"
    language "C#"
    kind "SharedLib"
  ]]

  project "IKVM.Reflection"
    SetupManagedProject()

    kind "SharedLib"
    language "C#"

    files { "../ikvm/reflect/**.cs" }
    links { "System", "System.Core", "System.Security" }

  group "Examples"

    print("Searching for example projects...")
    IncludeDir(examplesdir)

  group "Tests"

    print("Searching for tests projects...")
    IncludeDir(testsdir)