-- This is the starting point of the build scripts for the project.
-- It defines the common build settings that all the projects share
-- and calls the build scripts of all the sub-projects.

include "Helpers.lua"
include "Tests.lua"

newoption {
   trigger     = "outdir",
   value       = "path",
   description = "Output directory for the generated project files"
}

newoption {
   trigger     = "dev",
   value       = "bool",
   description = "Enables development mode"
}

workspace "MonoEmbeddinator4000"

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

  local action = _OPTIONS["outdir"] or _ACTION
  location (action)

  objdir (path.join("./", action, "obj"))
  targetdir (path.join("./", action, "lib", "%{cfg.buildcfg}"))

  startproject "MonoEmbeddinator4000"

  include ("../binder")

  function include_cppsharp_project(name)
    include("../external/CppSharp/src/" .. name)
  end

  include_cppsharp_project("Core")
  include_cppsharp_project("AST")
  include_cppsharp_project("CppParser/Bindings/CSharp")
  include_cppsharp_project("Parser")
  include_cppsharp_project("Generator")
  include_cppsharp_project("Runtime")

  if _OPTIONS["dev-cppsharp"] then
    include_cppsharp_project("Generator.Tests")
    include_cppsharp_project("../build/Tests")
    IncludeTests()
  end

  project "IKVM.Reflection"
    SetupManagedProject()

    kind "SharedLib"
    language "C#"

    files { "../external/ikvm/reflect/**.cs" }
    links { "System", "System.Core", "System.Security" }

  project "Xamarin.MacDev"
    SetupManagedProject()

    kind "SharedLib"
    language "C#"
    clr "Unsafe"

    files { "../external/Xamarin.MacDev/Xamarin.MacDev/**.cs" }
    links
    {
      "System",
      "System.Core",
      "System.Xml",
      "System.Xml.Linq",
      "Mono.Posix"
    }

  project "Xamarin.Android.Tools"
    SetupManagedProject()

    kind "SharedLib"
    language "C#"

    files { "../external/Xamarin.Android.Tools/src/Xamarin.Android.Tools/**.cs" }
    links
    {
      "System",
      "System.Core",
      "System.Xml",
      "System.Xml.Linq"
    }

  if string.startswith(_ACTION, "vs") and os.is("macosx") then
    externalproject "objcgen"
      SetupManagedProject()
      location "../objcgen"
      uuid "C166803B-011F-4EAF-B8C2-D7DBBA3CF1EC"
      kind "ConsoleApp"
  end

    local xamarinMacios = "../../xamarin-macios"
    if _OPTIONS["dev"] then
      --[[
      externalproject "mtouch"
        SetupManagedProject()
        location (path.join(xamarinMacios, "tools/mtouch"))
        uuid "A737EFCC-4348-4EB1-9C14-4FDC0975388D"
        kind "ConsoleApp"

      externalproject "Mono.Cecil"
        SetupManagedProject()
        location (path.join(xamarinMacios, "external/mono/external/cecil/"))
        uuid "D68133BD-1E63-496E-9EDE-4FBDBF77B486"
        kind "SharedLib"

      externalproject "Mono.Cecil.Mdb"
        SetupManagedProject()
        location (path.join(xamarinMacios, "external/mono/external/cecil/symbols/mdb"))
        uuid "8559DD7F-A16F-46D0-A05A-9139FAEBA8FD"
        kind "SharedLib"
      ]]

      print("Searching for tests projects...")
      IncludeDir("../tests")

    end