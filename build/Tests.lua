-- Tests/examples helpers

local supportdir = path.getabsolute("../support")
local catchdir = path.getabsolute("../external/catch")
local exepath = path.join("../../build/lib/Debug/MonoEmbeddinator4000.exe")

function SetupTestProject(name, extraFiles)
  objdir("!obj")
  targetdir "."

  SetupTestProjectGenerator()
  SetupTestProjectC(name)
  SetupTestProjectObjC(name)
  SetupTestProjectsCSharp(name, nil, extraFiles)
end

function SetupManagedTestProject()
    kind "SharedLib"
    language "C#"  
    clr "Unsafe"
    SetupManagedProject()
    location "."
end

function SetupTestProjectGenerator()
  if os.is("windows") then
    SetupTestProjectGeneratorVS(name)
  else
    SetupTestProjectGeneratorMake(name)
  end
end

function SetupTestProjectGeneratorMake()
  project (name .. ".Gen")
     location "."
     kind "Makefile"
     dependson (name .. ".Managed")

     buildcommands
     {
        "mono --debug " .. exepath .. " -gen=c -out=c -p=macos -compile -target=shared " .. name .. ".Managed.dll",
        "mono --debug " .. exepath .. " -gen=objc -out=objc -p=macos -compile -target=shared " .. name .. ".Managed.dll",
        "mono --debug " .. exepath .. " -gen=java -out=java -p=macos -target=shared " .. name .. ".Managed.dll"
     }
end

function SetupTestProjectGeneratorVS(name, depends)
  project(name .. ".Gen")
    SetupManagedTestProject()
    kind "ConsoleApp"
    
    files { name .. ".Gen.cs" }

    dependson { name .. ".Managed" }

    linktable = {
      "System.Core",
      "CppSharp.Generator",
      "MonoEmbeddinator4000",
    }

    if depends ~= nil then
      table.insert(linktable, depends .. ".Gen")
    end

    links(linktable)
end

function SetupTestGeneratorBuildEvent(name)
  local runtimeExe = os.is("windows") and "" or "mono --debug "
  if string.starts(action, "vs") then
    local exePath = SafePath("$(TargetDir)" .. name .. ".Gen.exe")
    prebuildcommands { runtimeExe .. exePath }
  else
    local exePath = SafePath("%{cfg.buildtarget.directory}/" .. name .. ".Gen.exe")
    prebuildcommands { runtimeExe .. exePath }
  end
end

local function SetupMono()
  local monoDir = nil

  -- Find system-specific Mono include/library paths.
  -- For Windows, first search the default Mono install location.
  local monoDefaultWindowsDir = "C:\\Program Files (x86)\\Mono"
  if os.isdir(monoDefaultWindowsDir) then
    monoDir = monoDefaultWindowsDir
  end

  local monoDefaultOSXDir = "/Library/Frameworks/Mono.framework/Versions/Current/"
  if os.isdir(monoDefaultOSXDir) then
    monoDir = monoDefaultOSXDir
  end

  -- TODO: Use premake-pkgconfig for Linux

  if not monoDir or not os.isdir(monoDir) then
    error("Could not find Mono install location, please specify it manually")
  end

  includedirs { path.join(monoDir, "include", "mono-2.0") }
  libdirs { path.join(monoDir, "lib") }
  links { "monosgen-2.0" }

  filter { "system:macosx" }
    links { "CoreFoundation.framework" }

  filter {}
end

function SetupTestProjectC(name, depends)
  if string.starts(action, "vs") and not os.is("windows") then
    return
  end

  project(name .. ".C")
    SetupNativeProject()
    location "."

    kind "SharedLib"
    language "C"

    defines { "MONO_DLL_IMPORT", "MONO_M2N_DLL_EXPORT" }

    flags { common_flags }
    files
    {
      path.join("c", name .. ".Managed.h"),
      path.join("c", name .. ".Managed.c"),
      path.join(supportdir, "*.h"),
      path.join(supportdir, "*.c"),
    }

    includedirs { supportdir }

    dependson { name .. ".Gen" }

    if depends ~= nil then
      links { depends .. ".C" }
    end

    filter { "action:vs*" }
      buildoptions { "/wd4018" } -- eglib signed/unsigned warnings

    filter {}

    SetupMono()
end

function SetupTestProjectObjC(name, depends)
  if string.starts(action, "vs") and not os.is("windows") then
    return
  end

  project(name .. ".ObjC")
    SetupNativeProject()
    location "."

    kind "SharedLib"
    language "C++"

    defines { "MONO_DLL_IMPORT", "MONO_M2N_DLL_EXPORT" }

    flags { common_flags }
    files
    {
      path.join("objc", name .. ".Managed.h"),
      path.join("objc", name .. ".Managed.mm"),
      path.join(supportdir, "*.h"),
      path.join(supportdir, "*.c"),
    }

    links { "objc" }

    includedirs { supportdir }

    dependson { name .. ".Gen" }

    SetupMono()
end

function SetupTestProjectsCSharp(name, depends, extraFiles)
  project(name .. ".Managed")
    SetupManagedTestProject()

    files
    {
      files { name .. ".cs" }
    }
    if extraFiles ~= nil then
      for _, file in pairs(extraFiles) do
        files { file .. ".cs" }
      end
    end

    linktable = { "System", "System.Core" }

    if depends ~= nil then
      table.insert(linktable, depends .. ".Managed")
    end

    links(linktable)

  project(name .. ".Tests")
    SetupNativeProject()
    location "."

    language "C++"
    kind "ConsoleApp"

    includedirs
    {
      path.join("c"),
      path.join(catchdir, "include"),
      supportdir
    }

    files
    {
      name .. ".Tests.*",
      path.join(supportdir, "glib.*"),
    }

    links { name .. ".C", name .. ".ObjC" }
    links { "objc", "CoreFoundation.framework", "Foundation.framework" }

    dependson { name .. ".Managed" }

    filter { "action:vs*" }
      buildoptions { "/wd4018" } -- eglib signed/unsigned warnings

    filter {}    
end
