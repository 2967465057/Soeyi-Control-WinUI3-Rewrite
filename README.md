# SOEYI Control — WinUI 3 Rewrite

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![WinUI](https://img.shields.io/badge/WinUI-3-0078D7?logo=windows)](https://learn.microsoft.com/windows-apps/windows-app-sdk/)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)]()

USB 副屏管理软件的 WinUI 3 Fluent Design 重写版本。从原始 SOEYI 反编译重建，完整兼容原版主题系统，并加入硬件监控、天气、深色模式等现代特性。

## 截图

> 运行 `SoeyiWinUI-v2.exe` 查看实际效果

## 功能

### 核心
- **USB 副屏驱动** — DLS (COM3, 921600 baud) / AIC 双协议，320×1480 @ 30fps 实时推流
- **硬件监控** — CPU/GPU 温度占用、RAM、网络速率，FanControl IPC 优先 + WMI 回退
- **实时天气** — Open-Meteo API，WMO 天气码转中文，时段感知描述
- **系统托盘** — 最小化到托盘、右键菜单、关闭提示、开机自启

### 主题系统
- **Programme 主题** — 完全兼容原始 SOEYI Setting.txt 格式
- **内置仪表盘** — CPU 占用/温度/功率、GPU 占用/温度、RAM、网络、天气渲染器
- **主题编辑器** — 元素增删排序、X/Y 拖拽定位、字号独立控制、居中/可见性开关、实时副屏预览
- **导入/导出** — ZIP 打包（JSON + Setting.txt + 素材），跨设备迁移
- **深色模式** — 全界面自适应，标题栏按钮反色跟随
- **自定义主题** — DIY 创建、裁剪背景图、另存为

### 其他
- 多语言：简体中文 / English / 日本語 / 한국어
- 配置持久化（JSON）
- 自动检测系统主题切换
- MSIX 打包支持

## 快速开始

### 环境

- Windows 10 1809+ 或 Windows 11
- [.NET SDK 10.0](https://dotnet.microsoft.com/download)
- Windows App SDK 2.2（NuGet 自动拉取）

### 编译运行

```powershell
git clone https://github.com/2967465057/Soeyi-Control-WinUI3-Rewrite.git
cd Soeyi-Control-WinUI3-Rewrite

# 编译
dotnet build SoeyiWinUI-v2.csproj -c Release

# 运行
dotnet run SoeyiWinUI-v2.csproj

# 或直接启动
.\bin\Release\net10.0-windows10.0.19041.0\win-x64\SoeyiWinUI-v2.exe
```

### MSIX 打包

```powershell
# 1. 生成签名证书
.\build-cert.ps1
certutil -addstore -f Root SoeyiCert.cer

# 2. 构建 MSIX
.\build-msix.cmd

# 3. 安装
Add-AppxPackage SoeyiWinUI.msix
```

## 项目结构

```
├── App.xaml(.cs)          # 应用入口，窗口创建，标题栏设置
├── Views/
│   └── MainPage.cs        # 主界面 — NavigationView 三页布局，主题编辑器
├── ViewModels/
│   └── MainViewModel.cs   # 数据绑定，80+ 翻译键，命令
├── Services/
│   ├── DeviceService.cs   # USB 设备检测、帧推流
│   ├── HardwareMonitorService.cs  # 硬件采样（FanControl/WMI）
│   ├── ThemeService.cs    # 主题发现、解析、导入导出
│   ├── WeatherService.cs  # Open-Meteo 天气
│   └── ConfigService.cs   # 配置持久化
├── Models/
│   └── SoeyiTheme.cs      # 主题 + SettingElement 模型
├── Rendering/
│   └── ThemeRenderer.cs   # SkiaSharp 渲染引擎
├── Native/
│   ├── deviceapi.cs       # DLS USB 显示 SDK 封装
│   ├── aicdeviceapi.cs    # AIC USB 显示 SDK 封装
│   └── cpuinfosdk.cs      # CPUID SDK 封装
├── NativeDlls/x64/        # 原生依赖
├── libusb/                # USB 驱动文件
├── SoeyiPackage/          # MSIX 打包项目
├── Assets/                # 应用图标资源
├── build.cmd              # 编译脚本
├── build-msix.cmd         # MSIX 构建脚本
└── build-cert.ps1         # 证书生成脚本
```

## 许可

本项目基于原始 SOEYI 软件逆向工程重建，仅用于学习研究。原始软件版权归其作者所有。
