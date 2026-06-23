# SOEYI WinUI v2

WinUI 3 Fluent Design USB secondary display management software, reverse-engineered and rebuilt from SOEYI.

## Prerequisites

- .NET SDK 10.0 (or 8.0 with minor .csproj adjustments)
- Windows 10/11 with Windows App SDK 2.2
- Visual Studio 2022+ (optional, CLI build works)

## Build (Unpackaged)

```powershell
dotnet build SoeyiWinUI-v2.csproj -c Release
dotnet run SoeyiWinUI-v2.csproj
```

## MSIX Packaging

```powershell
# 1. Generate self-signed certificate (one-time)
$cert = New-SelfSignedCertificate -Type Custom -Subject "CN=Soeyi" -KeyUsage DigitalSignature -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3")
$pwd = ConvertTo-SecureString -String "soeyi123" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "SoeyiCert.pfx" -Password $pwd

# 2. Install cert to trusted root (admin required)
certutil -addstore -f Root SoeyiCert.cer

# 3. Publish
dotnet publish SoeyiWinUI-v2.csproj -c Release -r win-x64 --self-contained -o publish

# 4. Create MSIX layout
robocopy publish msix-layout /E /NFL /NDL
Copy-Item Assets\* msix-layout\Assets\ -Force

# 5. Package & sign
makeappx pack /d msix-layout /p SoeyiWinUI.msix /o
signtool sign /fd SHA256 /f SoeyiCert.pfx /p soeyi123 SoeyiWinUI.msix

# 6. Install
Add-AppxPackage SoeyiWinUI.msix
```

## Features

- USB secondary display streaming (DLS/AIC, COM port 921600 baud)
- Hardware monitoring (CPU/GPU/RAM/Network via FanControl IPC + WMI fallback)
- Real-time weather (Open-Meteo API)
- Theme system (Programme themes, built-in dashboards, light/dark mode)
- Theme editor (add/delete/reorder/center elements, label visibility, font sizes)
- Theme import/export (ZIP + JSON)
- Multi-language (zh-CN, en-US, ja-JP, ko-KR)
- System tray + auto-start
- Config persistence

## License

Reverse-engineered for educational purposes.
