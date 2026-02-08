@echo off
setlocal enabledelayedexpansion

REM ============================
REM CONFIG
REM ============================
set CONFIG=Release
set ROOT=%~dp0
set BUILDS=%ROOT%Builds

REM --- Android SDK (generic)
set "ANDROID_SDK_ROOT=%LOCALAPPDATA%\Android\Sdk"
set "ANDROID_HOME=%ANDROID_SDK_ROOT%"

REM ============================
REM ARG PARSING
REM ============================
REM Usage:
REM   build.bat
REM   build.bat 1 | windows
REM   build.bat 2 | android
REM   build.bat 3 | both

set TARGET=%1

REM --- Normalize numeric args
if "%TARGET%"=="1" set TARGET=windows
if "%TARGET%"=="2" set TARGET=android
if "%TARGET%"=="3" set TARGET=both

REM --- Validate or fallback to menu
if "%TARGET%"=="" goto :menu

if /I not "%TARGET%"=="windows" if /I not "%TARGET%"=="android" if /I not "%TARGET%"=="both" (
    echo Invalid argument: %1
    echo Usage: build.bat [1|2|3|windows|android|both]
    exit /b 1
)

goto :build

:menu
echo.
echo Select app to build:
echo   1) Windows
echo   2) Android
echo   3) Both
echo.
set /p CHOICE=Enter choice [1-3]: 

if "%CHOICE%"=="1" set TARGET=windows
if "%CHOICE%"=="2" set TARGET=android
if "%CHOICE%"=="3" set TARGET=both

if "%TARGET%"=="" goto :menu

:build

REM ============================
REM PREPARE OUTPUT DIRS
REM ============================
if not exist "%BUILDS%" mkdir "%BUILDS%"

if /I "%TARGET%"=="windows" mkdir "%BUILDS%\Windows" 2>nul
if /I "%TARGET%"=="android" mkdir "%BUILDS%\Android" 2>nul
if /I "%TARGET%"=="both" (
    mkdir "%BUILDS%\Windows" 2>nul
    mkdir "%BUILDS%\Android" 2>nul
)

REM ============================
REM WINDOWS BUILD
REM ============================
if /I "%TARGET%"=="windows" goto :build_windows
if /I "%TARGET%"=="both" goto :build_windows
goto :after_windows

:build_windows
echo.
echo ============================
echo Publishing LogiG733Tray (Windows)
echo ============================

dotnet publish "%ROOT%LogiG733Tray\LogiG733Tray.csproj" ^
 -c %CONFIG% ^
 -r win-x64 ^
 --self-contained true ^
 -o "%BUILDS%\Windows" ^
 /bl:"%BUILDS%\Windows\LogiG733Tray.binlog"

if errorlevel 1 goto :error

:after_windows

REM ============================
REM ANDROID BUILD
REM ============================
if /I "%TARGET%"=="android" goto :build_android
if /I "%TARGET%"=="both" goto :build_android
goto :done

:build_android
echo.
echo ============================
echo Publishing LogiTrayAndroid (Android)
echo ============================

dotnet publish "%ROOT%LogiTrayAndroid\LogiTrayAndroid.csproj" ^
 -c %CONFIG% ^
 -f net10.0-android ^
 /p:AndroidPackageFormat=aab ^
 -o "%BUILDS%\Android"

if errorlevel 1 goto :error

goto :done

REM ============================
REM END
REM ============================
:done
echo.
echo Build complete
echo Output directory:
echo   %BUILDS%
exit /b 0

:error
echo.
echo Build failed
exit /b 1
