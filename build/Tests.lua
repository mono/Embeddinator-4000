-- Tests/examples helpers

local supportdir = path.getabsolute("../support")
local catchdir = path.getabsolute("../external/catch")
local exepath = "../../../build/lib/Debug/Embeddinator-4000.exe"

function SetupManagedTestProject()
    kind "SharedLib"
    language "C#"  
    clr "Unsafe"
    location "mk"
end

function SetupTestGeneratorBuildEvent(name)
  local runtimeExe = os.ishost("windows") and "" or "mono --debug "
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
    monoIncludeDir = path.join(monoDefaultWindowsDir, "include")
    monoLibDir = path.join(monoDefaultWindowsDir, "lib")
  end

  local monoDefaultOSXDir = "/Library/Frameworks/Mono.framework/Versions/Current/"
  if os.isdir(monoDefaultOSXDir) then
    monoIncludeDir = path.join(monoDefaultOSXDir, "include")
    monoLibDir = path.join(monoDefaultOSXDir, "lib")
  end

  -- TODO: Use premake-pkgconfig for Linux
  if os.ishost("linux") then
    monoIncludeDir = "/usr/include"
    monoLibDir = "/usr/lib"
  end

  if not monoIncludeDir or not os.isdir(monoIncludeDir) then
    error("Could not find Mono install location, please specify it manually")
  end

  includedirs { path.join(monoIncludeDir, "mono-2.0") }
  libdirs { monoLibDir }
  
  if(os.ishost("windows")) then
    libFiles = os.matchfiles(path.join(monoLibDir, "monosgen-2.0.lib"))
    table.insert(libFiles, os.matchfiles(path.join(monoLibDir, "mono-2.0-sgen.lib")))
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

    filter { "not system:windows" }
      buildoptions { "-std=gnu99" }

    filter { "action:vs*" }
      buildoptions { "/wd4018" } -- eglib signed/unsigned warnings

    filter {}

    SetupMono()
end

function SetupTestProjectObjC(name, depends)
  if string.starts(action, "vs") and not os.ishost("windows") then
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
  managed_project(name .. ".Managed")
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
