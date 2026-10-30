@echo off
rem Wswjq Voice build wrapper: bypasses PowerShell execution policy for build.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
exit /b %ERRORLEVEL%
