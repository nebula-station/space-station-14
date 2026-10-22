@echo off
chcp 65001 >nul
cd /d "%~dp0"

where py >nul 2>nul
if %errorlevel% equ 0 (
    py -3 RUN_THIS.py %*
) else (
    python RUN_THIS.py %*
)
if errorlevel 1 pause
