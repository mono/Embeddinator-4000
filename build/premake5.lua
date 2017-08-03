-- This is the starting point of the build scripts for the project.
-- It defines the common build settings that all the projects share
-- and calls the build scripts of all the sub-projects.

include "Helpers.lua"

newoption {
   trigger     = "outdir",
   value       = "path",
   description = "Output directory for the generated project files"
}

function managed_project(name)
  if name ~= nil then
    local proj = project(name)
  end

  language "C#"

  if not os.istarget("macosx") then
    filter { "action:vs*" }
      location "."
    filter {}
  end

  return proj
end

workspace "Embeddinator-4000"

  configurations { "Debug", "Release" }
  architecture "x86_64"
  dotnetframework "4.6"

  filter "system:windows"
    architecture "x86"

  filter "system:macosx"
    architecture "x86"

  filter "configurations:Release"
    optimize "On"

  filter {}

  characterset "Unicode"
  symbols "On"

  local action = _OPTIONS["outdir"] or _ACTION
  location (action)

  objdir (path.join("./", action, "obj"))
  targetdir (path.join("./", action, "lib", "%{cfg.buildcfg}"))

  startproject "Embeddinator-4000"

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

  managed_project "IKVM.Reflection"

    kind "SharedLib"
    language "C#"

    files { "../external/ikvm/reflect/**.cs" }
    links { "System", "System.Core", "System.Security" }

  managed_project "Xamarin.MacDev"

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

  managed_project "Xamarin.Android.Tools"

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

