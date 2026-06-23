@echo off
echo === SOEYI WinUI v2 Build ===
echo.

dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found
    exit /b 1
)

echo [1/2] Building project...
dotnet build SoeyiWinUI-v2.csproj -c Release
if %errorlevel% neq 0 (
    echo BUILD FAILED
    exit /b 1
)

echo [2/2] Build complete!
echo Run: dotnet run SoeyiWinUI-v2.csproj
