@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0启动爪账.ps1"
if errorlevel 1 pause
