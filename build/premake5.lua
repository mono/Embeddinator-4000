-- This is the starting point of the build scripts for the project.
-- It defines the common build settings that all the projects share
-- and calls the build scripts of all the sub-projects.

dofile "Helpers.lua"
dofile "Tests.lua"

solution "MonoManagedToNative"

  configurations { "Debug", "Release" }
  architecture "x86_64"

  filter "system:macosx"
    architecture "x86"

  filter "configurations:Release"
    flags { "Optimize" }    

  filter {}  

  characterset "Unicode"
  symbols "On"
  
  location (builddir)
  objdir (path.join(builddir, "obj"))
  targetdir (libdir)
  libdirs { libdir }

  startproject "MonoManagedToNative"

  include ("../binder")
  include("../CppSharp/src/Core")
  include("../CppSharp/src/AST")
  include("../CppSharp/src/Parser")
  include("../CppSharp/src/CppParser/Bindings")
  include("../CppSharp/src/Generator")
  include("../CppSharp/src/Runtime")

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