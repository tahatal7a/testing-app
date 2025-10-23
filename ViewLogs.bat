@echo off
echo Opening log file...
echo.
set LOGPATH=%APPDATA%\DesktopTaskAid
echo Log folder: %LOGPATH%
echo.

if exist "%LOGPATH%" (
    start "" "%LOGPATH%"
    echo Log folder opened in Explorer
    echo.
    echo Log files are named: app_log_YYYYMMDD.txt
    echo.
    pause
) else (
    echo ERROR: Log folder does not exist yet.
    echo The folder will be created when you run the app for the first time.
    echo.
    pause
)
