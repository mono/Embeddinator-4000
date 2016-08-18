-- This module checks for the all the project dependencies.

action = _ACTION or ""

depsdir = path.getabsolute("../deps");
srcdir = path.getabsolute("../src");
incdir = path.getabsolute("../include");
bindir = path.getabsolute("../bin");
examplesdir = path.getabsolute("../examples");
testsdir = path.getabsolute("../tests");
supportdir = path.getabsolute("../support");

builddir = path.getabsolute("./" .. action);
libdir = path.join(builddir, "lib", "%{cfg.buildcfg}");
gendir = path.join(builddir, "gen");

function string.starts(str, start)
   return string.sub(str, 1, string.len(start)) == start
end

function SafePath(path)
  return "\"" .. path .. "\""
end

msvc_buildflags = {  }
gcc_cpp_buildflags = { "-std=c++11 -fpermissive" }

msvc_cpp_defines = { }

function SetupNativeProject()
  location (path.join(builddir, "projects"))

  local c = configuration "Debug"
    defines { "DEBUG" }
    
  configuration "Release"
    defines { "NDEBUG" }
    optimize "On"
    
  -- Compiler-specific options
  
  configuration "vs*"
    buildoptions { msvc_buildflags }
    defines { msvc_cpp_defines }

  filter { "action:gmake", "language:c++" }
    buildoptions { gcc_cpp_buildflags }
    
  filter { "system:macosx", "language:c++" }
    buildoptions { gcc_cpp_buildflags, "-stdlib=libc++" }
    links { "c++" }
  
  -- OS-specific options
  
  configuration "Windows"
    defines { "WIN32", "_WINDOWS" }
  
  configuration(c)
end

function SetupManagedProject()
  dotnetframework "4.6"

  location (path.join(builddir, "projects"))

  local c = configuration "vs*"
    location "."

  configuration(c)
end

function IncludeDir(dir)
  local deps = os.matchdirs(dir .. "/*")
  
  for i,dep in ipairs(deps) do
    local fp = path.join(dep, "premake5.lua")
    fp = path.join(os.getcwd(), fp)
    
    if os.isfile(fp) then
      include(dep)
      return
    end    

    fp = path.join(dep, "premake4.lua")
    fp = path.join(os.getcwd(), fp)
    
    if os.isfile(fp) then
      --print(string.format(" including %s", dep))
      include(dep)
    end
  end
end