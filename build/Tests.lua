-- Tests/examples helpers

local supportdir = path.getabsolute("../support")
local catchdir = path.getabsolute("../external/catch")
local exepath = "../../../build/lib/Debug/Embeddinator-4000.exe"

function SetupTestProject(name, extraFiles)
  objdir("!obj")
  targetdir "."

  SetupTestProjectGenerator(name)
  SetupTestProjectC(name)
  SetupTestProjectObjC(name)
  SetupTestProjectsCSharp(name, nil, extraFiles)
  SetupTestProjectsRunner(name)
end

function SetupManagedTestProject()
    kind "SharedLib"
    language "C#"  
    clr "Unsafe"
    SetupManagedProject()
    location "mk"
end

function SetupTestProjectGenerator(name)
  if os.is("windows") then
    SetupTestProjectGeneratorVS(name)
  else
    SetupTestProjectGeneratorMake(name)
  end
end

function SetupTestProjectGeneratorMake(name, dll)
  project (name .. ".Gen")
     location "mk"
     kind "Makefile"
     dependson (name .. ".Managed")

     if dll == nil then
       dll = name .. "Managed.dll"
     end

     buildcommands
     {
        "mono --debug " .. exepath .. " -gen=c -out=c -p=macos -compile -target=shared " .. dll,
        "mono --debug " .. exepath .. " -gen=objc -out=objc -p=macos -compile -target=shared " .. dll,
        "mono --debug " .. exepath .. " -gen=java -out=java -p=macos -target=shared " .. dll
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

function SetupMono()
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
  monoLibdir = path.join(monoDir, "lib")
  libdirs { monoLibdir }
  if(os.is("windows")) then
    libFiles = os.matchfiles(path.join(monoLibdir, "monosgen-2.0.lib"))
    table.insert(libFiles, os.matchfiles(path.join(monoLibdir, "mono-2.0-sgen.lib")))
    links { libFiles }
  else
    links { "monosgen-2.0" }
  end
  
  filter { "system:macosx" }
    links { "CoreFoundation.framework" }

  filter {}
end

function SetupTestProjectC(name, depends)
  -- if string.starts(action, "vs") and not os.is("windows") then
    -- return
  -- end

  project(name .. ".C")

    kind "SharedLib"
    language "C"

    defines { "MONO_EMBEDDINATOR_DLL_EXPORT", "MONO_DLL_IMPORT"}

    flags { common_flags }
    files
    {
      path.join("c", "*.h"),
      path.join("c", "*.c"),
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

    kind "SharedLib"
    language "C"

    defines { "MONO_DLL_IMPORT", "MONO_M2N_DLL_EXPORT" }

    flags { common_flags }
    files
    {
      path.join("objc", "*.h"),
      path.join("objc", "*.mm"),
      path.join("objc", "*.c"),
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
end

function SetupTestProjectsRunner(name)
  project(name .. ".Tests")

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
      "*Tests.*",
      path.join(supportdir, "glib.*"),
    }

    links { name .. ".C"}
    filter { "macosx" }
      links { name .. ".ObjC", "objc", "CoreFoundation.framework", "Foundation.framework" }

    dependson { name .. ".Managed" }

    filter { "action:vs*" }
      buildoptions { "/wd4018" } -- eglib signed/unsigned warnings

    filter {}  
end
