@echo off
setlocal

cd /d "%~dp0"

set VENV_DIR=.venv
set PYTHON=%VENV_DIR%\Scripts\python.exe
set PIP=%VENV_DIR%\Scripts\pip.exe

if not exist "%VENV_DIR%" (
    echo Creating virtual environment...
    python -m venv "%VENV_DIR%"
    if errorlevel 1 exit /b 1
)

echo Installing dependencies...
"%PIP%" install -r requirements.txt
if errorlevel 1 exit /b 1

echo.
echo Starting GameMaster Console Client...
"%PYTHON%" gamemaster_client.py

pause
