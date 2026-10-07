@echo off
chcp 65001 >nul
cd /d "%~dp0build"
if exist "Anti Ăn Toxic (AUT).exe" (
    start "" "Anti Ăn Toxic (AUT).exe"
) else if exist "LOL Key.exe" (
    start "" "LOL Key.exe"
) else (
    start "" dotnet "Anti Ăn Toxic (AUT).dll"
)
exit
