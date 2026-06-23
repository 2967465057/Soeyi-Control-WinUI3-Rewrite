@echo off
echo === SOEYI WinUI v2 MSIX Package ===

if not exist "SoeyiCert.pfx" (
    echo Run build-cert.ps1 first
    exit /b 1
)

echo [1/4] Publishing...
dotnet publish SoeyiWinUI-v2.csproj -c Release -r win-x64 --self-contained -o publish
if %errorlevel% neq 0 exit /b 1

echo [2/4] Layout...
if exist msix-layout rmdir /s /q msix-layout
mkdir msix-layout
robocopy publish msix-layout /E /NFL /NDL
xcopy /Y /Q Assets\* msix-layout\Assets\

echo [3/4] Manifest...
copy /Y SoeyiPackage\Package.appxmanifest msix-layout\AppxManifest.xml

echo [4/4] Package & Sign...
makeappx pack /d msix-layout /p SoeyiWinUI.msix /o
signtool sign /fd SHA256 /f SoeyiCert.pfx /p soeyi123 SoeyiWinUI.msix

echo Done: SoeyiWinUI.msix
