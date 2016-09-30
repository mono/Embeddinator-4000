-- This is the starting point of the build scripts for the project.
-- It defines the common build settings that all the projects share
-- and calls the build scripts of all the sub-projects.

include "Helpers.lua"
include "Tests.lua"

solution "MonoManagedToNative"

  configurations { "Debug", "Release" }
  architecture "x86_64"

  filter "system:windows"
    architecture "x86"

  filter "system:macosx"
    architecture "x86"

  filter "configurations:Release"
    flags { "Optimize" }    

  filter {}  

  characterset "Unicode"
  symbols "On"

  local action = _ACTION or ""
  location (action)

  objdir (path.join("./", action, "obj"))
  targetdir (path.join("./", action, "lib", "%{cfg.buildcfg}"))

  startproject "MonoManagedToNative"

  include ("../binder")
  include("../CppSharp/src/Core")
  include("../CppSharp/src/AST")
  include("../CppSharp/src/CppParser/Bindings/CSharp")
  include("../CppSharp/src/Parser")
  include("../CppSharp/src/Generator")
  include("../CppSharp/src/Runtime")

  project "IKVM.Reflection"
    SetupManagedProject()

    kind "SharedLib"
    language "C#"

    files { "../ikvm/reflect/**.cs" }
    links { "System", "System.Core", "System.Security" }

  group "Tests"

    print("Searching for tests projects...")
    IncludeDir("../tests")