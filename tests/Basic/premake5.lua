group "Tests/Basic"

name="Basic"
exepath = path.join("../../build/lib/Debug/MonoEmbeddinator4000.exe")

objdir("!obj")
targetdir "."

SetupTestNativeProject(name)  
SetupTestProjectsCSharp(name)

project (name .. ".Gen")
   location "."
   kind "Makefile"
   dependson (name .. ".Managed")

   buildcommands {
      "mono --debug " .. exepath .. " -gen=c -out=c -p=macos -compile -target=shared " .. name .. ".Managed.dll",
      "mono --debug " .. exepath .. " -gen=objc -out=objc -p=macos -compile -target=shared " .. name .. ".Managed.dll",
      "mono --debug " .. exepath .. " -gen=java -out=java -p=macos -target=shared " .. name .. ".Managed.dll"
   }
