@echo off
echo === SOEYI WinUI v2 MSIX Package ===

if not exist "SoeyiCert.pfx" (
    echo Run build-cert.ps1 first
    exit /b 1
)

echo [1/5] Publishing (framework-dependent)...
dotnet publish SoeyiWinUI-v2.csproj -c Release -r win-x64 -o publish
if %errorlevel% neq 0 exit /b 1

echo [2/5] Extracting SxS manifest for WinRT COM registration...
for /f "delims=" %%i in ('dir /s /b "C:\Program Files (x86)\Windows Kits\10\bin\*x64\mt.exe" 2^>nul') do set MT=%%i
if defined MT (
    "%MT%" -inputresource:publish\SoeyiWinUI-v2.exe -out:publish\SoeyiWinUI-v2.exe.manifest
    echo   Manifest extracted
) else (
    echo   mt.exe not found, trying built-in extraction...
    powershell -Command "& { $m=[System.Reflection.Assembly]::LoadFile([System.IO.Path]::Combine($pwd,'publish\SoeyiWinUI-v2.exe')); }" 2>nul
)

echo [3/5] Layout...
if exist msix-layout rmdir /s /q msix-layout
mkdir msix-layout
robocopy publish msix-layout /E /NFL /NDL
xcopy /Y /Q Assets\* msix-layout\Assets\
REM Also copy libusb folder for driver auto-install
if exist libusb\ robocopy libusb msix-layout\libusb /E /NFL /NDL

echo [4/5] Manifest...
copy /Y SoeyiPackage\Package.appxmanifest msix-layout\AppxManifest.xml

echo [5/5] Package ^& Sign...
makeappx pack /d msix-layout /p SoeyiWinUI.msix /o
signtool sign /fd SHA256 /f SoeyiCert.pfx /p soeyi123 SoeyiWinUI.msix

echo Done: SoeyiWinUI.msix