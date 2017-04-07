include "Helpers.lua"
include "../../build/Tests.lua"

workspace "Basic"
  configurations { "Debug" }
  symbols "On"
  location "mk"

  --SetupTestProjectGeneratorMake("Basic", "managed.dll")
  SetupTestProjectC("Basic")
  SetupTestProjectObjC("Basic")
  SetupTestProjectsRunner("Basic")