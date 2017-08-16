include "Helpers.lua"
include "../../build/Tests.lua"

workspace "mk"
  configurations { "Debug" }
  symbols "On"
  location "mk"
  SetupTestProjectC("common")
  if os.ishost("macosx") then
  SetupTestProjectObjC("common")
  end
  SetupTestProjectsRunner("common")
  SetupMono()