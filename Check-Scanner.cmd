@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Invoke-Scan.ps1" -Action List
pause
