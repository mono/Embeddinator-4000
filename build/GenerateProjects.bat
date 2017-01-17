@echo off
goto menu

:menu
echo Build Project Generator:
echo.
echo [0] Clean
echo [1] Visual C++ 2015
echo [2] GNU Make
echo.

:choice
set /P C="Choice: "
if "%C%"=="2" goto gmake
if "%C%"=="1" goto vs2015
if "%C%"=="0" goto clean

:clean
"..\CppSharp\build\premake5" --file=premake5.lua clean
goto quit

:vs2015
"..\CppSharp\build\premake5" --file=premake5.lua --outdir=. --os=macosx vs2015
goto quit

:gmake
"..\CppSharp\build\premake5" --file=premake5.lua gmake
goto quit

:quit
pause
:end