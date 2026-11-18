@echo off
rem ============================================================
rem  start1.exe build script (pure ASCII, safe for cmd.exe)
rem  Prefers bundled zig cc; falls back to gcc / MSVC cl.
rem ============================================================

setlocal
cd /d "%~dp0"

set "ZIG=%~dp0..\tools\ziglang\ziglang\zig.exe"
set "OUT=start1.exe"

if exist "%ZIG%" (
  echo [build] using zig cc: %ZIG%
  "%ZIG%" cc -target x86_64-windows-gnu -O2 -municode start1.c -o "%OUT%" -lcomctl32 -lshell32 -luser32 -lgdi32
  goto :done
)

where gcc >nul 2>&1
if %errorlevel%==0 (
  echo [build] using gcc
  gcc -municode -O2 start1.c -o "%OUT%" -lcomctl32 -lshell32 -luser32 -lgdi32
  goto :done
)

where cl >nul 2>&1
if %errorlevel%==0 (
  echo [build] using MSVC cl
  cl /nologo /O2 /DUNICODE /D_UNICODE start1.c /Fe:"%OUT%" comctl32.lib shell32.lib user32.lib gdi32.lib
  goto :done
)

echo [ERROR] No C compiler found. Put zig.exe into ..\tools\ziglang\ziglang\ or install gcc/MSVC.
exit /b 1

:done
if exist "%OUT%" (
  echo [OK] built: %~dp0%OUT%
) else (
  echo [ERROR] build failed.
  exit /b 1
)
endlocal
