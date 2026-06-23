#define DEBUG
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using SoeyiWinUI_v2.Models;
using SoeyiWinUI_v2.Rendering;
using SoeyiWinUI_v2.Services;
namespace SoeyiWinUI_v2.ViewModels;

public class MainViewModel : ObservableObject, IDisposable
{
	private readonly DeviceService _deviceService;

	public DeviceService? DeviceSvc { get; }

	private readonly ConfigService _configService;

	public readonly ThemeService ThemeService;

	private readonly HardwareMonitorService _monitor;

	private readonly DispatcherQueue _dq;

	private ObservableCollection<DeviceInfo> _attachedDevices = new ObservableCollection<DeviceInfo>();

	private DeviceInfo? _selectedDevice;

	private HwSnapshot _hardwareInfo = new HwSnapshot(0f, 0f, 0f, 0f, 0f, 0uL, 0f, 0f);

	private SoeyiTheme _currentTheme = null;

	private bool _isDarkTheme;

	private bool _isDriverRunning;

	private string _statusMessage = "Ready";

	private bool _startMinimized;

	private bool _useFanControl = true;

	private int _brightness = 80;

	private int _volume = 50;

	private int _rotation;

	private int _compressionQuality = 85;

	private string _currentLang = "zh-CN";

	public static Dictionary<string, Dictionary<string, string>> Loc;

	private AsyncRelayCommand? startDriverCommand;

	private AsyncRelayCommand? stopDriverCommand;

	private RelayCommand<object?>? setBrightnessCommand;

	private RelayCommand<object?>? setVolumeCommand;

	private RelayCommand<int>? setRotationCommand;

	private RelayCommand<int>? setQualityCommand;

	private RelayCommand<string>? selectThemeCommand;

	private RelayCommand? toggleDarkModeCommand;

	private RelayCommand? saveSettingsCommand;

	private RelayCommand? toggleStartMinimizedCommand;

	private RelayCommand<string>? setLanguageCommand;

	public int JpegQuality
	{
		get
		{
			return CompressionQuality;
		}
		set
		{
			CompressionQuality = value;
		}
	}

	public string SelectedLanguage
	{
		get
		{
			return CurrentLang;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(CurrentLang, value))
			{
				CurrentLang = value;
				OnPropertyChanged("SelectedLanguage");
			}
		}
	}

	public IReadOnlyList<SoeyiTheme> ThemePresets => ThemeService.Themes;

	public static int[] QualityLevels => new int[9] { 50, 60, 70, 75, 80, 85, 90, 95, 100 };

	public IRelayCommand SaveConfigCommand => SaveSettingsCommand;

	public ObservableCollection<DeviceInfo> AttachedDevices
	{
		get
		{
			return _attachedDevices;
		}
		set
		{
			if (!EqualityComparer<ObservableCollection<DeviceInfo>>.Default.Equals(_attachedDevices, value))
			{
				OnPropertyChanging("AttachedDevices");
				_attachedDevices = value;
				OnPropertyChanged("AttachedDevices");
			}
		}
	}

	public DeviceInfo? SelectedDevice
	{
		get
		{
			return _selectedDevice;
		}
		set
		{
			if (!EqualityComparer<DeviceInfo>.Default.Equals(_selectedDevice, value))
			{
				OnPropertyChanging("SelectedDevice");
				_selectedDevice = value;
				OnPropertyChanged("SelectedDevice");
			}
		}
	}

	public HwSnapshot HwSnapshot
	{
		get
		{
			return _hardwareInfo;
		}
		set
		{
			if (!EqualityComparer<SoeyiWinUI_v2.Services.HwSnapshot>.Default.Equals(_hardwareInfo, value))
			{
				OnPropertyChanging("HwSnapshot");
				_hardwareInfo = value;
				OnPropertyChanged("HwSnapshot");
			}
		}
	}

	public SoeyiTheme CurrentTheme
	{
		get
		{
			return _currentTheme;
		}
		set
		{
			if (!EqualityComparer<SoeyiTheme>.Default.Equals(_currentTheme, value))
			{
				OnPropertyChanging("CurrentTheme");
				_currentTheme = value;
				OnPropertyChanged("CurrentTheme");
			}
		}
	}

	public bool IsDarkTheme
	{
		get
		{
			return _isDarkTheme;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isDarkTheme, value))
			{
				OnPropertyChanging("IsDarkTheme");
				_isDarkTheme = value;
				OnPropertyChanged("IsDarkTheme");
			}
		}
	}

	public bool UseSystemTheme
	{
		get
		{
			return _configService.Get("UseSystemTheme", defaultValue: false);
		}
		set
		{
			_configService.Set("UseSystemTheme", value);
			_configService.Save();
			OnPropertyChanged("UseSystemTheme");
		}
	}

	public bool StartWithWindows
	{
		get
		{
			return _configService.Get("StartWithWindows", defaultValue: false);
		}
		set
		{
			_configService.Set("StartWithWindows", value);
			_configService.Save();
			SetAutoStart(value);
			OnPropertyChanged("StartWithWindows");
		}
	}

	public bool IsDriverRunning
	{
		get
		{
			return _isDriverRunning;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isDriverRunning, value))
			{
				OnPropertyChanging("IsDriverRunning");
				_isDriverRunning = value;
				OnPropertyChanged("IsDriverRunning");
			}
		}
	}

	public string StatusMessage
	{
		get
		{
			return _statusMessage;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_statusMessage, value))
			{
				OnPropertyChanging("StatusMessage");
				_statusMessage = value;
				OnPropertyChanged("StatusMessage");
			}
		}
	}

	public bool StartMinimized
	{
		get
		{
			return _startMinimized;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_startMinimized, value))
			{
				OnPropertyChanging("StartMinimized");
				_startMinimized = value;
				OnPropertyChanged("StartMinimized");
			}
		}
	}

	public bool UseFanControl
	{
		get
		{
			return _useFanControl;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_useFanControl, value))
			{
				OnPropertyChanging("UseFanControl");
				_useFanControl = value;
				_monitor.SetFanControl(value);
				OnPropertyChanged("UseFanControl");
			}
		}
	}

	public int Brightness
	{
		get
		{
			return _brightness;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_brightness, value))
			{
				OnPropertyChanging("Brightness");
				_brightness = value;
				OnPropertyChanged("Brightness");
			}
		}
	}

	public int Volume
	{
		get
		{
			return _volume;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_volume, value))
			{
				OnPropertyChanging("Volume");
				_volume = value;
				OnPropertyChanged("Volume");
			}
		}
	}

	public int Rotation
	{
		get
		{
			return _rotation;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_rotation, value))
			{
				OnPropertyChanging("Rotation");
				_rotation = value;
				OnPropertyChanged("Rotation");
			}
		}
	}

	public int CompressionQuality
	{
		get
		{
			return _compressionQuality;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_compressionQuality, value))
			{
				OnPropertyChanging("CompressionQuality");
				_compressionQuality = value;
				OnPropertyChanged("CompressionQuality");
			}
		}
	}

	public string CurrentLang
	{
		get
		{
			return _currentLang;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_currentLang, value))
			{
				OnPropertyChanging("CurrentLang");
				_currentLang = value;
				ThemeRenderer.Lang = value;
				OnPropertyChanged("CurrentLang");
				OnPropertyChanged("SelectedLanguage");
			}
		}
	}

	public IAsyncRelayCommand StartDriverCommand => startDriverCommand ?? (startDriverCommand = new AsyncRelayCommand(StartDriverAsync));

	public IAsyncRelayCommand StopDriverCommand => stopDriverCommand ?? (stopDriverCommand = new AsyncRelayCommand(StopDriver));

	public IRelayCommand<object?> SetBrightnessCommand => setBrightnessCommand ?? (setBrightnessCommand = new RelayCommand<object>(SetBrightness));

	public IRelayCommand<object?> SetVolumeCommand => setVolumeCommand ?? (setVolumeCommand = new RelayCommand<object>(SetVolume));

	public IRelayCommand<int> SetRotationCommand => setRotationCommand ?? (setRotationCommand = new RelayCommand<int>(SetRotation));

	public IRelayCommand<int> SetQualityCommand => setQualityCommand ?? (setQualityCommand = new RelayCommand<int>(SetQuality));

	public IRelayCommand<string> SelectThemeCommand => selectThemeCommand ?? (selectThemeCommand = new RelayCommand<string>(SelectTheme));

	public IRelayCommand ToggleDarkModeCommand => toggleDarkModeCommand ?? (toggleDarkModeCommand = new RelayCommand(ToggleDarkMode));

	public IRelayCommand SaveSettingsCommand => saveSettingsCommand ?? (saveSettingsCommand = new RelayCommand(SaveSettings));

	public IRelayCommand ToggleStartMinimizedCommand => toggleStartMinimizedCommand ?? (toggleStartMinimizedCommand = new RelayCommand(ToggleStartMinimized));

	public IRelayCommand<string> SetLanguageCommand => setLanguageCommand ?? (setLanguageCommand = new RelayCommand<string>(SetLanguage));

	private static void SetAutoStart(bool enable)
	{
		using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
		if (registryKey == null)
		{
			return;
		}
		string text = Environment.ProcessPath ?? Assembly.GetEntryAssembly()?.Location;
		if (text != null)
		{
			if (enable)
			{
				registryKey.SetValue("SoeyiWinUI", "\"" + text + "\" --minimized");
			}
			else
			{
				registryKey.DeleteValue("SoeyiWinUI", throwOnMissingValue: false);
			}
		}
	}

	public (bool isSet, bool exit) GetClosePreference()
	{
		string text = _configService.Get("CloseAction", "");
		bool flag = false;
		(bool, bool) result = ((text == "minimize") ? (true, false) : ((!(text == "exit")) ? (false, true) : (true, true)));
		bool flag2 = false;
		return result;
	}

	public void SetClosePreference(string action)
	{
		_configService.Set("CloseAction", action);
		_configService.Save();
	}

	public string T(string key)
	{
		if (Loc.TryGetValue(CurrentLang, out Dictionary<string, string> value) && value.TryGetValue(key, out var value2))
		{
			return value2;
		}
		if (Loc.TryGetValue("en-US", out Dictionary<string, string> value3) && value3.TryGetValue(key, out var value4))
		{
			return value4;
		}
		return key;
	}

	public MainViewModel(DeviceService deviceService, ConfigService configService, ThemeService themeService, HardwareMonitorService monitor)
	{
		_dq = DispatcherQueue.GetForCurrentThread();
		_deviceService = deviceService;
		_configService = configService;
		ThemeService = themeService;
		_monitor = monitor;
		_currentTheme = themeService.CurrentTheme;
		ThemeRenderer.Lang = _currentLang;
		_isDarkTheme = themeService.IsDarkTheme;
		LoadSettings();
		Task.Run(delegate
		{
			try
			{
				_monitor.Start();
			}
			catch
			{
			}
		});
		_deviceService.DeviceAttached += OnDeviceAttached;
		_deviceService.DeviceDetached += OnDeviceDetached;
		_monitor.Updated += OnHwUpdated;
	}

	public void Initialize()
	{
		StartDriver();
	}

	private void LoadSettings()
	{
		StartMinimized = _configService.Get("StartMinimized", defaultValue: false);
		UseFanControl = _configService.Get("UseFanControl", defaultValue: true);
		CompressionQuality = _configService.Get("CompressionQuality", 85);
		CurrentLang = _configService.Get("Language", "zh-CN");
	}

	private async Task StartDriverAsync()
	{
		try
		{
			int r = await Task.Run(() => _deviceService.Start());
			IsDriverRunning = r >= 0;
			StatusMessage = (IsDriverRunning ? T("driverRunning") : $"Start failed: {r}");
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Exception ex3 = ex2;
			Exception ex4 = ex3;
			Exception ex5 = ex4;
			StatusMessage = "Error: " + ex5.Message;
			Debug.WriteLine("Driver start failed: " + ex5.Message);
		}
	}

	public Task StartDriver()
	{
		return StartDriverAsync();
	}

	private async Task StopDriver()
	{
		await Task.Run(delegate
		{
			_monitor.Stop();
			_deviceService.Stop();
		});
		IsDriverRunning = false;
		AttachedDevices.Clear();
		StatusMessage = T("driverStopped");
	}

	private void SetBrightness(object? param)
	{
		bool flag = false;
		int num = ((param is double num2) ? ((int)num2) : ((!(param is int num3)) ? Brightness : num3));
		bool flag2 = false;
		int num4 = num;
		if (SelectedDevice != null)
		{
			_deviceService.SetBrightness(SelectedDevice.Handle, num4);
		}
		Brightness = num4;
	}

	private void SetVolume(object? param)
	{
		bool flag = false;
		int num = ((param is double num2) ? ((int)num2) : ((!(param is int num3)) ? Volume : num3));
		bool flag2 = false;
		int num4 = num;
		if (SelectedDevice != null)
		{
			_deviceService.SetVolume(SelectedDevice.Handle, num4);
		}
		Volume = num4;
	}

	private void SetRotation(int value)
	{
		if (SelectedDevice != null)
		{
			_deviceService.SetRotation(SelectedDevice.Handle, value);
		}
		Rotation = value;
	}

	private void SetQuality(int value)
	{
		CompressionQuality = value;
		_configService.Set("CompressionQuality", value);
		_configService.Save();
	}

	private void SelectTheme(string themeId)
	{
		ThemeService.SelectTheme(themeId);
		CurrentTheme = ThemeService.CurrentTheme;
	}

	private void ToggleDarkMode()
	{
		IsDarkTheme = !IsDarkTheme;
		ThemeService.IsDarkTheme = IsDarkTheme;
	}

	public void ApplyDarkTheme()
	{
		SolidColorBrush solidColorBrush = Application.Current.Resources["ContentDialogBackground"] as SolidColorBrush;
	}

	private void SaveSettings()
	{
		_configService.Set("StartMinimized", StartMinimized);
		_configService.Set("UseFanControl", UseFanControl);
		_configService.Set("CompressionQuality", CompressionQuality);
		_configService.Set("Language", CurrentLang);
		_configService.Save();
		StatusMessage = "Settings saved";
	}

	private void ToggleStartMinimized()
	{
		StartMinimized = !StartMinimized;
	}

	private void OnDeviceAttached(object? s, DeviceInfo d)
	{
		_dq.TryEnqueue(delegate
		{
			AttachedDevices.Add(d);
			if ((object)SelectedDevice == null)
			{
				DeviceInfo deviceInfo = (SelectedDevice = d);
				DeviceInfo deviceInfo3 = deviceInfo;
				DeviceInfo deviceInfo4 = deviceInfo3;
				DeviceInfo deviceInfo5 = deviceInfo4;
			}
		});
	}

	private void OnDeviceDetached(object? s, uint h)
	{
		_dq.TryEnqueue(delegate
		{
			DeviceInfo deviceInfo = AttachedDevices.FirstOrDefault((DeviceInfo x) => x.Handle == h);
			if (deviceInfo != null)
			{
				AttachedDevices.Remove(deviceInfo);
			}
		});
	}

	private void OnHwUpdated(HwSnapshot hw)
	{
		_dq.TryEnqueue(delegate
		{
			HwSnapshot = hw;
		});
	}

	public void Dispose()
	{
		_monitor.Updated -= OnHwUpdated;
		_deviceService.DeviceAttached -= OnDeviceAttached;
		_deviceService.DeviceDetached -= OnDeviceDetached;
	}

	private void SetLanguage(string lang)
	{
		CurrentLang = lang;
	}

	static MainViewModel()
	{
		Dictionary<string, Dictionary<string, string>> dictionary = new Dictionary<string, Dictionary<string, string>>
		{
			["zh-CN"] = new Dictionary<string, string>
			{
				["dashboard"] = "仪表盘",
				["monitor"] = "硬件监控",
				["theme"] = "主题",
				["settings"] = "设置",
				["import"] = "导入",
				["export"] = "导出",
				["saveAs"] = "另存为",
				["delete"] = "删除",
				["setDefault"] = "设默认",
				["saved"] = "已另存",
				["imported"] = "已导入",
				["jpeg"] = "JPEG画质",
				["exported"] = "已导出",
				["newTheme"] = "新建主题",
				["bgImage"] = "背景图片",
				["editorTitle"] = "名称",
				["editorX"] = "X偏移",
				["editorY"] = "Y偏移",
				["editorSave"] = "保存",
				["editorCancel"] = "取消",
				["editorText"] = "文字",
				["editorBar"] = "进度条",
				["editorSize"] = "字号",
				["darkMode"] = "深色模式",
				["followSystemTheme"] = "跟随系统",
				["autoStart"] = "开机自启",
				["closePrompt"] = "是否将程序最小化到系统托盘？",
				["connected"] = "已连接",
				["controls"] = "控制",
				["minimizeToTray"] = "最小化到托盘",
				["exitApp"] = "退出程序",
				["rememberChoice"] = "记住我的选择",
				["startMin"] = "开机最小化",
				["fanControl"] = "FanControl 数据源",
				["quality"] = "画质",
				["language"] = "语言",
				["devices"] = "设备",
				["driverRunning"] = "驱动运行中",
				["driverStopped"] = "驱动已停止",
				["brightness"] = "亮度",
				["volume"] = "音量",
				["rotation"] = "旋转",
				["cpuUsage"] = "CPU占用",
				["cpuTemp"] = "CPU温度",
				["gpuUsage"] = "GPU占用",
				["gpuTemp"] = "GPU温度",
				["ram"] = "内存",
				["memFreq"] = "内存频率",
				["network"] = "网络",
				["noDevice"] = "无设备",
				["ready"] = "就绪",
				["running"] = "运行中",
				["stopped"] = "已停止",
				["handle"] = "句柄",
				["serialNumber"] = "序列号",
				["save"] = "保存",
				["editorFs"] = "字号",
				["editorLfs"] = "标识字号",

				["editorVisible"] = "显示",

			["addIndicator"] = "添加指示器",				["curTime"] = "时间",
			["editorCenter"] = "居中",
			["editorLabelVis"] = "隐藏标识",


				["curDate"] = "日期",
				["curWeek"] = "星期",
				["editorThemeSize"] = "分辨率",
				["cpu"] = "处理器",
				["cpuFreq"] = "CPU频率",
				["nightWeather"] = "夜晚天气",
				["heightWeather"] = "白天天气",
				["lowWeather"] = "低温",
				["weatherInfo"] = "天气",
				["cpuPower"] = "CPU功耗",
				["gpuPower"] = "GPU功耗",
				["gpu"] = "显卡",
				["startDrv"] = "启动驱动",
				["stopDrv"] = "停止驱动",
				["start"] = "启动",
				["stop"] = "停止"
			},
			["en-US"] = new Dictionary<string, string>
			{
				["dashboard"] = "Dashboard",
				["monitor"] = "Monitor",
				["theme"] = "Theme",
				["settings"] = "Settings",
				["import"] = "Import",
				["export"] = "Export",
				["saveAs"] = "Save As",
				["delete"] = "Delete",
				["setDefault"] = "Set Default",
				["saved"] = "Saved",
				["imported"] = "Imported",
				["jpeg"] = "JPEG Quality",
				["exported"] = "Exported",
				["newTheme"] = "New Theme",
				["bgImage"] = "Background",
				["editorTitle"] = "Name",
				["editorX"] = "X Offset",
				["editorY"] = "Y Offset",
				["editorSave"] = "Save",
				["editorCancel"] = "Cancel",
				["editorText"] = "Text",
				["editorBar"] = "Bar",
				["editorSize"] = "Font Size",
				["darkMode"] = "Dark Mode",
				["followSystemTheme"] = "Follow System",
				["autoStart"] = "Auto Start",
				["closePrompt"] = "Minimize to system tray instead of closing?",
				["connected"] = "Connected",
				["controls"] = "Controls",
				["minimizeToTray"] = "Minimize to Tray",
				["exitApp"] = "Exit",
				["rememberChoice"] = "Remember my choice",
				["startMin"] = "Start Minimized",
				["fanControl"] = "FanControl Source",
				["quality"] = "Quality",
				["language"] = "Language",
				["devices"] = "Devices",
				["driverRunning"] = "Driver Running",
				["driverStopped"] = "Driver Stopped",
				["brightness"] = "Brightness",
				["volume"] = "Volume",
				["rotation"] = "Rotation",
				["cpuUsage"] = "CPU Usage",
				["cpuTemp"] = "CPU Temp",
				["gpuUsage"] = "GPU Usage",
				["gpuTemp"] = "GPU Temp",
				["ram"] = "RAM",
				["memFreq"] = "Memory Freq",
				["network"] = "Network",
				["noDevice"] = "No Device",
				["ready"] = "Ready",
				["running"] = "Running",
				["stopped"] = "Stopped",
				["handle"] = "Handle",
				["serialNumber"] = "Serial No",
				["save"] = "Save",
				["editorFs"] = "Font Size",
				["editorLfs"] = "Label Size",

				["editorVisible"] = "Visible",

			["addIndicator"] = "Add Indicator",				["curTime"] = "Time",
			["editorCenter"] = "Center",
			["editorLabelVis"] = "Hide Label",


				["curDate"] = "Date",
				["curWeek"] = "Weekday",
				["editorThemeSize"] = "Resolution",
				["cpu"] = "CPU",
				["cpuFreq"] = "CPU Freq",
				["nightWeather"] = "Night",
				["heightWeather"] = "Day",
				["lowWeather"] = "Low",
				["weatherInfo"] = "Weather",
				["cpuPower"] = "CPU Power",
				["gpuPower"] = "GPU Power",
				["gpu"] = "GPU",
				["startDrv"] = "Start Driver",
				["stopDrv"] = "Stop Driver",
				["start"] = "Start",
				["stop"] = "Stop"
			},
			["ja-JP"] = new Dictionary<string, string>
			{
				["dashboard"] = "ダッシュボード",
				["monitor"] = "モニター",
				["theme"] = "テーマ",
				["settings"] = "設定",
				["import"] = "インポート",
				["export"] = "エクスポート",
				["saveAs"] = "名前を付けて保存",
				["delete"] = "削除",
				["setDefault"] = "デフォルトに設定",
				["saved"] = "保存済",
				["imported"] = "インポート済",
				["jpeg"] = "JPEG品質",
				["exported"] = "エクスポート済",
				["newTheme"] = "新規テーマ",
				["bgImage"] = "背景画像",
				["editorTitle"] = "名前",
				["editorX"] = "Xオフセット",
				["editorY"] = "Yオフセット",
				["editorSave"] = "保存",
				["editorCancel"] = "キャンセル",
				["editorText"] = "テキスト",
				["editorBar"] = "バー",
				["editorSize"] = "フォントサイズ",
				["darkMode"] = "ダークモード",
				["followSystemTheme"] = "システムに従う",
				["autoStart"] = "自動起動",
				["closePrompt"] = "システムトレイに最小化しますか？",
				["connected"] = "接続済み",
				["controls"] = "コントロール",
				["minimizeToTray"] = "トレイに最小化",
				["exitApp"] = "終了",
				["rememberChoice"] = "選択を記憶する",
				["startMin"] = "最小化起動",
				["fanControl"] = "FanControl ソース",
				["quality"] = "画質",
				["language"] = "言語",
				["devices"] = "デバイス",
				["driverRunning"] = "ドライバ実行中",
				["driverStopped"] = "ドライバ停止",
				["brightness"] = "輝度",
				["volume"] = "音量",
				["rotation"] = "回転",
				["cpuUsage"] = "CPU使用率",
				["cpuTemp"] = "CPU温度",
				["gpuUsage"] = "GPU使用率",
				["gpuTemp"] = "GPU温度",
				["ram"] = "メモリ",
				["memFreq"] = "メモリ周波数",
				["network"] = "ネットワーク",
				["noDevice"] = "デバイスなし",
				["ready"] = "準備完了",
				["running"] = "実行中",
				["stopped"] = "停止",
				["handle"] = "ハンドル",
				["serialNumber"] = "シリアル番号",
				["save"] = "保存",
				["editorFs"] = "フォントサイズ",
				["editorLfs"] = "ラベルサイズ",

				["editorVisible"] = "表示",

			["addIndicator"] = "インジケーター追加",				["curTime"] = "時間",
			["editorCenter"] = "中央",
			["editorLabelVis"] = "ラベル非表示",


				["curDate"] = "日付",
				["curWeek"] = "曜日",
				["editorThemeSize"] = "解像度",
				["cpu"] = "CPU",
				["cpuFreq"] = "CPU周波数",
				["nightWeather"] = "夜天気",
				["heightWeather"] = "昼天気",
				["lowWeather"] = "低温",
				["weatherInfo"] = "天気",
				["cpuPower"] = "CPU消費電力",
				["gpuPower"] = "GPU消費電力",
				["gpu"] = "GPU",
				["startDrv"] = "ドライバ起動",
				["stopDrv"] = "ドライバ停止",
				["start"] = "開始",
				["stop"] = "停止"
			}
		};
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		dictionary2["dashboard"] = "대시보드";
		dictionary2["monitor"] = "모니터";
		dictionary2["theme"] = "테마";
		dictionary2["settings"] = "설정";
		dictionary2["import"] = "가져오기";
		dictionary2["export"] = "내보내기";
		dictionary2["saveAs"] = "다른이름저장";
		dictionary2["delete"] = "삭제";
		dictionary2["setDefault"] = "기본설정";
		dictionary2["saved"] = "저장됨";
		dictionary2["imported"] = "가져옴";
		dictionary2["jpeg"] = "JPEG 화질";
		dictionary2["exported"] = "내보냄";
		dictionary2["newTheme"] = "새 테마";
		dictionary2["bgImage"] = "배경 이미지";
		dictionary2["editorTitle"] = "이름";
		dictionary2["editorX"] = "X 오프셋";
		dictionary2["editorY"] = "Y 오프셋";
		dictionary2["editorSave"] = "저장";
		dictionary2["editorCancel"] = "취소";
		dictionary2["editorText"] = "텍스트";
		dictionary2["editorBar"] = "바";
		dictionary2["editorSize"] = "글자크기";
		dictionary2["darkMode"] = "다크모드";
		dictionary2["followSystemTheme"] = "시스템따르기";
		dictionary2["autoStart"] = "자동시작";
		dictionary2["closePrompt"] = "시스템 트레이로 최소화할까요?";
		dictionary2["connected"] = "연결됨";
		dictionary2["controls"] = "컨트롤";
		dictionary2["minimizeToTray"] = "트레이로 최소화";
		dictionary2["exitApp"] = "종료";
		dictionary2["rememberChoice"] = "선택 기억하기";
		dictionary2["startMin"] = "최소화시작";
		dictionary2["fanControl"] = "FanControl 소스";
		dictionary2["quality"] = "화질";
		dictionary2["language"] = "언어";
		dictionary2["devices"] = "장치";
		dictionary2["driverRunning"] = "드라이버실행중";
		dictionary2["driverStopped"] = "드라이버중지";
		dictionary2["brightness"] = "밝기";
		dictionary2["volume"] = "볼륨";
		dictionary2["rotation"] = "회전";
		dictionary2["cpuUsage"] = "CPU사용률";
		dictionary2["cpuTemp"] = "CPU온도";
		dictionary2["gpuUsage"] = "GPU사용률";
		dictionary2["gpuTemp"] = "GPU온도";
		dictionary2["ram"] = "메모리";
		dictionary2["memFreq"] = "메모리주파수";
		dictionary2["cpuFreq"] = "CPU주파수";
		dictionary2["cpuPower"] = "CPU전력";
		dictionary2["gpuPower"] = "GPU전력";
		dictionary2["nightWeather"] = "야간날씨";
		dictionary2["heightWeather"] = "주간날씨";
		dictionary2["lowWeather"] = "저온";
		dictionary2["weatherInfo"] = "날씨";
		dictionary2["network"] = "네트워크";
		dictionary2["noDevice"] = "장치없음";
		dictionary2["ready"] = "준비됨";
		dictionary2["running"] = "실행중";
		dictionary2["stopped"] = "중지됨";
		dictionary2["handle"] = "핸들";
		dictionary2["serialNumber"] = "시리얼번호";
		dictionary2["save"] = "저장";
		dictionary2["editorFs"] = "글자크기";
		dictionary2["editorLfs"] = "라벨크기";

				dictionary2["editorVisible"] = "표시";

		dictionary2["addIndicator"] = "지표 추가";
		dictionary2["editorCenter"] = "중앙";
		dictionary2["editorLabelVis"] = "라벨 숨기기";



		dictionary2["curDate"] = "날짜";
		dictionary2["curWeek"] = "요일";
		dictionary2["editorThemeSize"] = "해상도";
		dictionary2["cpu"] = "CPU";
		dictionary2["cpuPower"] = "CPU전력";
		dictionary2["gpuPower"] = "GPU전력";
		dictionary2["cpuPower"] = "CPU Power";
		dictionary2["gpuPower"] = "GPU Power";
		dictionary2["gpu"] = "GPU";
		dictionary2["startDrv"] = "드라이버시작";
		dictionary2["stopDrv"] = "드라이버중지";
		dictionary2["start"] = "시작";
		dictionary2["stop"] = "중지";
		dictionary["ko-KR"] = dictionary2;
		Loc = dictionary;
	}
}