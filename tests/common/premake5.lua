include "Helpers.lua"
include "../../build/Tests.lua"

workspace ""
  configurations { "Debug" }
  symbols "On"
  location "mk"

  SetupTestProjectC("common")
  SetupTestProjectObjC("common")
  SetupTestProjectsRunner("common")
  SetupMono()