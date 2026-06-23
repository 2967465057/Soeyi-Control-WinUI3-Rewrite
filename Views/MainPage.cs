#define DEBUG
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using SkiaSharp;
using SoeyiWinUI_v2.Models;
using SoeyiWinUI_v2.Rendering;
using SoeyiWinUI_v2.Services;
using SoeyiWinUI_v2.ViewModels;
using WinRT.Interop;
using Windows.Foundation;
using Windows.UI;

namespace SoeyiWinUI_v2.Views;

public sealed class MainPage : Page
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	private struct OpenFileName
	{
		public int lStructSize;

		public nint hwndOwner;

		public nint hInstance;

		public string? lpstrFilter;

		public string? lpstrCustomFilter;

		public int nMaxCustFilter;

		public int nFilterIndex;

		public string? lpstrFile;

		public int nMaxFile;

		public string? lpstrFileTitle;

		public int nMaxFileTitle;

		public string? lpstrInitialDir;

		public string? lpstrTitle;

		public int Flags;

		public short nFileOffset;

		public short nFileExtension;

		public string? lpstrDefExt;

		public nint lCustData;

		public nint lpfnHook;

		public string? lpTemplateName;
	}

	private struct POINT
	{
		public int x;

		public int y;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct WNDCLASSEXW
	{
		public uint cbSize;

		public uint style;

		public nint lpfnWndProc;

		public int cbClsExtra;

		public int cbWndExtra;

		public nint hInstance;

		public nint hIcon;

		public nint hCursor;

		public nint hbrBackground;

		public string? lpszMenuName;

		public string lpszClassName;

		public nint hIconSm;
	}

	private delegate nint WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct NOTIFYICONDATAW
	{
		public int cbSize;

		public nint hWnd;

		public uint uID;

		public uint uFlags;

		public uint uCallbackMessage;

		public nint hIcon;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string szTip;
	}

	private readonly MainViewModel _vm;

	private readonly List<(TextBlock t, string key)> _loc = new List<(TextBlock, string)>();

	private NavigationViewItem _dashItem;

	private NavigationViewItem _themeItem;

	private NavigationViewItem _settingsItem;
	private Button _newThemeBtn;
	private Button _bgImageBtn;
	private Button _importBtn;
	private Button _exportBtn;
	private Button _saveAsBtn;
	private Button _saveBtn2;
	private Button _delBtn;

	private TextBlock _badgeText;

	private TextBlock _footerText;

	private TextBlock _deviceCountBlock;

	private TextBlock _driverStatusBlock;

	private TextBlock _brightVal;

	private TextBlock _volVal;

	private TextBlock _cpuVal;

	private TextBlock _gpuVal;

	private TextBlock _ramVal;

	private TextBlock _netVal;

	private ToggleSwitch _darkToggle;

	private ToggleSwitch _startMinToggle;

	private ComboBox _qualityCombo;

	private ComboBox _langCombo;

	private TextBlock _currentThemeLabel;

	private MenuFlyout _themeFlyout;

	private DropDownButton _themeDropDown;

	private HwSnapshot _lastHw = new HwSnapshot(0f, 0f, 0f, 0f, 0f, 0uL, 0f, 0f);

	private Grid _titleBarGrid;

	private Slider _brightSlider;

	private Slider _volSlider;

	private StackPanel _devicePanel;

	private Button _startBtn;

	private Button _stopBtn;

	private Button _saveBtn;

	private readonly List<TextBlock> _hwLabels = new List<TextBlock>();

	private readonly SolidColorBrush Acc = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 0, 120, 212));

	private readonly SolidColorBrush Wht = new SolidColorBrush(Color.FromArgb(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));

	private readonly SolidColorBrush CBg = new SolidColorBrush(Color.FromArgb(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));

	private readonly SolidColorBrush TSec = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 115, 115, 115));

	private readonly SolidColorBrush PBg = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 243, 246, 250));

	private readonly SolidColorBrush Div = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 215, 220, 228));

	private readonly SolidColorBrush Txt = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 30, 30, 30));

	private readonly List<Border> _cards = new List<Border>();

	private readonly List<TextBlock> _labels = new List<TextBlock>();

	private string? _cropBgPath = null;

	private SKBitmap? _cropBgBitmap = null;

	private float _cropW;

	private float _cropH;

	private float _cropX;

	private float _cropY;

	private string? _cropName = null;

	private int _cropThemeW;

	private int _cropThemeH;

	private int _cropPw;

	private int _cropPh;

	private DispatcherTimer? _cropTimer;

	private Image? _cropPreviewImg;

	private Image? _themePreviewImg;

	private DispatcherTimer? _previewRefreshTimer;

	private TextBlock? _cropInfoLbl;

	private Slider? _cropZoom;

	private Slider? _cropPX;

	private Slider? _cropPY;

	private Grid? _cropOverlay;

	private const int IDI_APPLICATION = 32512;

	private const uint NIM_ADD = 0u;

	private const uint NIM_DELETE = 2u;

	private const uint NIF_MESSAGE = 1u;

	private const uint NIF_ICON = 2u;

	private const uint NIF_TIP = 4u;

	private const uint WM_LBUTTONUP = 514u;

	private const uint WM_RBUTTONUP = 517u;

	private const uint WM_COMMAND = 273u;

	private const uint WM_TRAY_CALLBACK = 32769u;

	private const int TPM_RIGHTBUTTON = 2;

	private const int TPM_BOTTOMALIGN = 4;

	private const uint MF_STRING = 0u;

	private nint _trayMsgWnd;

	private bool _trayCreated;

	private string _trayClassName = "";

	private static WndProcDelegate? _trayProcDel;

	private static nint _mainHwndStatic;

	private static AppWindow? _trayAppWindow;

	internal static Action? _trayShowAction;

	internal static Action? _trayExitAction;

	private bool _settingThemeProgrammatically;

	public UIElement TitleBarElement => _titleBarGrid;

	private void SetColorScheme(bool dark)
	{
		CBg.Color = (dark ? Color.FromArgb(byte.MaxValue, 40, 40, 44) : Color.FromArgb(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));
		TSec.Color = (dark ? Color.FromArgb(byte.MaxValue, 160, 160, 170) : Color.FromArgb(byte.MaxValue, 115, 115, 115));
		PBg.Color = (dark ? Color.FromArgb(byte.MaxValue, 28, 28, 32) : Color.FromArgb(byte.MaxValue, 243, 246, 250));
		Div.Color = (dark ? Color.FromArgb(byte.MaxValue, 60, 60, 68) : Color.FromArgb(byte.MaxValue, 215, 220, 228));
		Txt.Color = (dark ? Color.FromArgb(byte.MaxValue, 230, 230, 240) : Color.FromArgb(byte.MaxValue, 30, 30, 30));
		base.Background = PBg;
		base.RequestedTheme = ((!dark) ? ElementTheme.Light : ElementTheme.Dark);
		App._titleBarThemeUpdater?.Invoke(dark);
	}

	public MainPage(MainViewModel vm)
	{
		_vm = vm;
		SetColorScheme(dark: false);
		BuildUI();
		BindViewModel();
		ApplyLocalization();
		base.Loaded += OnLoaded;
	}

	[DllImport("shell32.dll", SetLastError = true)]
	private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATAW lpData);

	[DllImport("user32.dll")]
	private static extern nint LoadIcon(nint hInstance, int lpIconName);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern nint LoadImage(nint hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

	private const uint IMAGE_ICON = 1u;
	private const uint LR_LOADFROMFILE = 0x0010u;

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern nint CreateWindowEx(uint dwExStyle, nint lpClassName, string lpWindowName, uint dwStyle, int X, int Y, int nWidth, int nHeight, nint hWndParent, nint hMenu, nint hInstance, nint lpParam);

	[DllImport("user32.dll")]
	private static extern bool DestroyWindow(nint hWnd);

	[DllImport("user32.dll")]
	private static extern nint DefWindowProc(nint hWnd, uint Msg, nint wParam, nint lParam);

	[DllImport("kernel32.dll")]
	private static extern nint GetModuleHandle(string? lpModuleName);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern ushort RegisterClassEx(ref WNDCLASSEXW lpwcx);

	[DllImport("user32.dll")]
	private static extern bool UnregisterClass(string lpClassName, nint hInstance);

	[DllImport("user32.dll")]
	private static extern nint CreatePopupMenu();

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern bool AppendMenuW(nint hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

	[DllImport("user32.dll")]
	private static extern int TrackPopupMenu(nint hMenu, uint uFlags, int x, int y, int nReserved, nint hWnd, nint prcRect);

	[DllImport("user32.dll")]
	private static extern bool DestroyMenu(nint hMenu);

	[DllImport("user32.dll")]
	private static extern bool GetCursorPos(out POINT lpPoint);

	[DllImport("user32.dll")]
	private static extern nint SetForegroundWindow(nint hWnd);

	[DllImport("user32.dll")]
	private static extern void PostMessageW(nint hWnd, uint Msg, nuint wParam, nint lParam);

	private static nint TrayWndProc(nint hWnd, uint msg, nint wParam, nint lParam)
	{
		if (msg == 32769)
		{
			if ((int)lParam == 514)
			{
				Task.Run(delegate
				{
					try
					{
						_trayAppWindow?.Show(activateWindow: true);
					}
					catch
					{
					}
				});
				return 0;
			}
			if ((int)lParam == 517)
			{
				ShowTrayMenu(hWnd);
				return 0;
			}
		}
		if (msg == 273)
		{
			switch ((uint)((int)wParam & 0xFFFF))
			{
			case 1u:
				_trayShowAction?.Invoke();
				break;
			case 2u:
				_trayExitAction?.Invoke();
				break;
			}
			return 0;
		}
		return DefWindowProc(hWnd, msg, wParam, lParam);
	}

	private static void ShowTrayMenu(nint hWnd)
	{
		nint num = CreatePopupMenu();
		if (num != 0)
		{
			nint hMenu = num;
			string lang = ThemeRenderer.Lang;
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			bool flag5 = false;
			if (1 == 0)
			{
			}
			string text = lang switch
			{
				"zh-CN" => "打开面板", 
				"ja-JP" => "パネルを開く", 
				"ko-KR" => "패널 열기", 
				_ => "Open Panel", 
			};
			if (1 == 0)
			{
			}
			string text2 = text;
			bool flag6 = false;
			string text3 = text2;
			bool flag7 = false;
			string text4 = text3;
			bool flag8 = false;
			string text5 = text4;
			bool flag9 = false;
			string lpNewItem = text5;
			bool flag10 = false;
			AppendMenuW(hMenu, 0u, 1u, lpNewItem);
			nint hMenu2 = num;
			string lang2 = ThemeRenderer.Lang;
			bool flag11 = false;
			bool flag12 = false;
			bool flag13 = false;
			bool flag14 = false;
			bool flag15 = false;
			if (1 == 0)
			{
			}
			text = lang2 switch
			{
				"zh-CN" => "退出程序", 
				"ja-JP" => "終了", 
				"ko-KR" => "종료", 
				_ => "Exit", 
			};
			if (1 == 0)
			{
			}
			text2 = text;
			bool flag16 = false;
			text3 = text2;
			bool flag17 = false;
			text4 = text3;
			bool flag18 = false;
			text5 = text4;
			bool flag19 = false;
			lpNewItem = text5;
			bool flag20 = false;
			AppendMenuW(hMenu2, 0u, 2u, lpNewItem);
			SetForegroundWindow(hWnd);
			GetCursorPos(out var lpPoint);
			TrackPopupMenu(num, 6u, lpPoint.x, lpPoint.y, 0, hWnd, 0);
			PostMessageW(hWnd, 0u, 0u, 0);
			DestroyMenu(num);
		}
	}

	internal void CreateTrayIcon(nint mainHwnd, AppWindow appWindow)
	{
		if (_trayCreated)
		{
			return;
		}
		_trayClassName = "SoeyiTrayWnd";
		_trayProcDel = TrayWndProc;
		_mainHwndStatic = mainHwnd;
		_trayAppWindow = appWindow;
		nint moduleHandle = GetModuleHandle(null);
		WNDCLASSEXW lpwcx = new WNDCLASSEXW
		{
			cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
			lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_trayProcDel),
			hInstance = moduleHandle,
			lpszClassName = _trayClassName
		};
		ushort num = RegisterClassEx(ref lpwcx);
		if (num != 0)
		{
			_trayMsgWnd = CreateWindowEx(0u, num, "", 0u, 0, 0, 0, 0, 0, 0, moduleHandle, 0);
			if (_trayMsgWnd != 0)
			{
				NOTIFYICONDATAW lpData = new NOTIFYICONDATAW
				{
					cbSize = Marshal.SizeOf<NOTIFYICONDATAW>(),
					hWnd = _trayMsgWnd,
					uID = 1u,
					uFlags = 7u,
					uCallbackMessage = 32769u,
					hIcon = LoadImage(0, System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Assets", "Soeyi32.ico"), IMAGE_ICON, 0, 0, LR_LOADFROMFILE),
					szTip = "SOEYI WinUI"
				};
				_trayCreated = Shell_NotifyIcon(0u, ref lpData);
			}
		}
	}

	internal void RemoveTrayIcon()
	{
		if (_trayCreated)
		{
			NOTIFYICONDATAW lpData = new NOTIFYICONDATAW
			{
				cbSize = Marshal.SizeOf<NOTIFYICONDATAW>(),
				hWnd = _trayMsgWnd,
				uID = 1u
			};
			Shell_NotifyIcon(2u, ref lpData);
			_trayCreated = false;
		}
		if (_trayMsgWnd != 0)
		{
			DestroyWindow(_trayMsgWnd);
			_trayMsgWnd = 0;
		}
		if (!string.IsNullOrEmpty(_trayClassName))
		{
			UnregisterClass(_trayClassName, GetModuleHandle(null));
			_trayClassName = "";
		}
	}

	internal async Task<bool> ShowCloseDialog(bool rememberIsSet, bool rememberChoice)
	{
		if (rememberIsSet)
		{
			return rememberChoice;
		}
		CheckBox rememberCb = new CheckBox
		{
			Content = _vm.T("rememberChoice"),
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
		};
		ContentDialog dlg = new ContentDialog
		{
			Title = "\ud83d\udeaa SOEYI WinUI",
			Content = new StackPanel
			{
				Children = 
				{
					(UIElement)new TextBlock
					{
						Text = _vm.T("closePrompt"),
						TextWrapping = TextWrapping.Wrap
					},
					(UIElement)rememberCb
				}
			},
			PrimaryButtonText = _vm.T("minimizeToTray"),
			SecondaryButtonText = _vm.T("exitApp"),
			CloseButtonText = _vm.T("editorCancel"),
			DefaultButton = ContentDialogButton.Primary,
			XamlRoot = base.XamlRoot
		};
		ContentDialogResult result = await dlg.ShowAsync();
		if (rememberCb.IsChecked == true)
		{
			_vm.SetClosePreference((result == ContentDialogResult.Primary) ? "minimize" : "exit");
		}
		if (1 == 0)
		{
		}
		bool result2 = result switch
		{
			ContentDialogResult.Primary => false, 
			ContentDialogResult.Secondary => true, 
			_ => true, 
		};
		if (1 == 0)
		{
		}
		return result2;
	}

	private static string FmtSpeed(float kbps)
	{
		if (!(kbps < 999.5f))
		{
			float num = kbps / 1000f;
			if (num < 999.5f)
			{
				return $"{num:F1} M";
			}
			return $"{num / 1000f:F2} G";
		}
		return $"{kbps:F0} K";
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		base.Loaded -= OnLoaded;
		DispatcherTimer dispatcherTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(2L)
		};
		dispatcherTimer.Tick += delegate
		{
			if (_vm.UseSystemTheme)
			{
				ApplySystemTheme();
			}
		};
		dispatcherTimer.Start();
		if (_vm.UseSystemTheme)
		{
			ApplySystemTheme();
		}
		_vm.StartDriver();
	}

	private void ApplySystemTheme()
	{
		if (_settingThemeProgrammatically)
		{
			return;
		}
		using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
		int num = (registryKey?.GetValue("AppsUseLightTheme") as int?) ?? 1;
		bool flag = num == 0;
		if (_vm.IsDarkTheme != flag)
		{
			_settingThemeProgrammatically = true;
			try
			{
				_vm.ToggleDarkModeCommand.Execute(null);
				return;
			}
			finally
			{
				_settingThemeProgrammatically = false;
			}
		}
	}

	private TextBlock L(string key, double size = 14.0, bool bold = false)
	{
		TextBlock textBlock = new TextBlock
		{
			FontSize = size,
			FontWeight = (bold ? FontWeights.SemiBold : FontWeights.Normal),
			VerticalAlignment = VerticalAlignment.Center
		};
		_loc.Add((textBlock, key));
		return textBlock;
	}

	private void BuildUI()
	{
		Grid grid = new Grid();
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid.RowDefinitions.Add(new RowDefinition());
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		Grid obj = new Grid
		{
			Height = 48.0,
			Padding = new Thickness(16.0, 0.0, 140.0, 0.0)
		};
		Grid grid2 = obj;
		_titleBarGrid = obj;
		Grid grid3 = grid2;
		grid3.BorderThickness = new Thickness(0.0, 0.0, 0.0, 1.0);
		grid3.BorderBrush = Div;
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 8.0
		};
		stackPanel.Children.Add(new FontIcon
		{
			Glyph = "\ue7f4",
			FontSize = 18.0,
			Foreground = Acc
		});
		stackPanel.Children.Add(new TextBlock
		{
			Text = "SOEYI WinUI",
			FontSize = 16.0,
			FontWeight = FontWeights.SemiBold
		});
		Grid.SetColumn(stackPanel, 0);
		grid3.Children.Add(stackPanel);
		Border border = new Border
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Padding = new Thickness(12.0, 4.0, 12.0, 4.0),
			CornerRadius = new CornerRadius(12.0),
			Background = Acc
		};
		_badgeText = new TextBlock
		{
			Text = _vm.T("ready"),
			Foreground = Wht,
			FontSize = 12.0
		};
		border.Child = _badgeText;
		Grid.SetColumn(border, 1);
		grid3.Children.Add(border);
		Grid.SetRow(grid3, 0);
		grid.Children.Add(grid3);
		NavigationView navigationView = new NavigationView
		{
			PaneDisplayMode = NavigationViewPaneDisplayMode.Auto,
			IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed,
			IsSettingsVisible = false,
			OpenPaneLength = 200.0
		};
		NavigationViewItem navigationViewItem = new NavigationViewItem
		{
			Content = _vm.T("dashboard"),
			Tag = "dashboard"
		};
		navigationViewItem.Icon = new FontIcon
		{
			Glyph = "\ue7f4"
		};
		_dashItem = navigationViewItem;
		navigationView.MenuItems.Add(navigationViewItem);
		NavigationViewItem navigationViewItem2 = new NavigationViewItem
		{
			Content = _vm.T("theme"),
			Tag = "theme"
		};
		navigationViewItem2.Icon = new FontIcon
		{
			Glyph = "\ue771"
		};
		_themeItem = navigationViewItem2;
		navigationView.MenuItems.Add(navigationViewItem2);
		NavigationViewItem navigationViewItem3 = new NavigationViewItem
		{
			Content = _vm.T("settings"),
			Tag = "settings"
		};
		navigationViewItem3.Icon = new FontIcon
		{
			Glyph = "\ue713"
		};
		_settingsItem = navigationViewItem3;
		navigationView.MenuItems.Add(navigationViewItem3);
		ScrollViewer dashScroll = new ScrollViewer
		{
			Margin = new Thickness(16.0, 12.0, 16.0, 12.0)
		};
		StackPanel stackPanel2 = new StackPanel
		{
			Spacing = 16.0
		};
		Grid grid4 = new Grid
		{
			ColumnSpacing = 16.0
		};
		grid4.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(2.0, GridUnitType.Star),
			MinWidth = 200.0
		});
		grid4.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(3.0, GridUnitType.Star),
			MinWidth = 240.0
		});
		StackPanel stackPanel3 = new StackPanel
		{
			Spacing = 12.0
		};
		StackPanel stackPanel4 = new StackPanel
		{
			Spacing = 12.0
		};
		Grid.SetColumn(stackPanel3, 0);
		Grid.SetColumn(stackPanel4, 1);
		grid4.Children.Add(stackPanel3);
		grid4.Children.Add(stackPanel4);
		stackPanel2.Children.Add(grid4);
		dashScroll.Content = stackPanel2;
		ScrollViewer themeScroll = new ScrollViewer
		{
			Margin = new Thickness(16.0, 12.0, 16.0, 12.0),
			Visibility = Visibility.Collapsed
		};
		StackPanel stackPanel5 = new StackPanel
		{
			Spacing = 12.0
		};
		themeScroll.Content = stackPanel5;
		ScrollViewer setScroll = new ScrollViewer
		{
			Margin = new Thickness(16.0, 12.0, 16.0, 12.0),
			Visibility = Visibility.Collapsed
		};
		StackPanel stackPanel6 = new StackPanel
		{
			Spacing = 12.0
		};
		setScroll.Content = stackPanel6;
		Grid grid5 = new Grid();
		grid5.Children.Add(dashScroll);
		grid5.Children.Add(themeScroll);
		grid5.Children.Add(setScroll);
		navigationView.Content = grid5;
		navigationView.SelectionChanged += delegate(NavigationView _, NavigationViewSelectionChangedEventArgs args)
		{
			string text = (args.SelectedItem as NavigationViewItem)?.Tag?.ToString();
			dashScroll.Visibility = ((!(text == "dashboard")) ? Visibility.Collapsed : Visibility.Visible);
			themeScroll.Visibility = ((!(text == "theme")) ? Visibility.Collapsed : Visibility.Visible);
			setScroll.Visibility = ((!(text == "settings")) ? Visibility.Collapsed : Visibility.Visible);
		};
		Grid.SetRow(navigationView, 1);
		grid.Children.Add(navigationView);
		navigationView.SelectedItem = navigationViewItem;
		Grid grid6 = new Grid
		{
			Height = 32.0,
			Padding = new Thickness(12.0, 0.0, 12.0, 0.0)
		};
		grid6.BorderThickness = new Thickness(0.0, 1.0, 0.0, 0.0);
		grid6.BorderBrush = Div;
		_footerText = new TextBlock
		{
			Text = _vm.T("ready"),
			FontSize = 12.0,
			VerticalAlignment = VerticalAlignment.Center,
			Foreground = TSec
		};
		_labels.Add(_footerText);
		grid6.Children.Add(_footerText);
		_deviceCountBlock = new TextBlock
		{
			Text = _vm.T("devices") + ": 0",
			FontSize = 12.0,
			Foreground = TSec
		};
		_labels.Add(_deviceCountBlock);
		_driverStatusBlock = new TextBlock
		{
			Text = _vm.T("stopped"),
			FontSize = 12.0,
			Foreground = TSec
		};
		_labels.Add(_driverStatusBlock);
		StackPanel stackPanel7 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 16.0,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		stackPanel7.Children.Add(_deviceCountBlock);
		stackPanel7.Children.Add(_driverStatusBlock);
		grid6.Children.Add(stackPanel7);
		Grid.SetRow(grid6, 2);
		grid.Children.Add(grid6);
		base.Content = grid;
		BuildDisplays(stackPanel3);
		BuildControls(stackPanel3);
		BuildMonitor(stackPanel4);
		BuildTheme(stackPanel5);
		BuildSettings(stackPanel6);
	}

	private void BuildDisplays(StackPanel left)
	{
		Border border = new Border
		{
			CornerRadius = new CornerRadius(12.0),
			Padding = new Thickness(16.0),
			Background = CBg
		};
		_cards.Add(border);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 12.0
		};
		stackPanel.Children.Add(L("connected", 18.0, bold: true));
		_devicePanel = new StackPanel
		{
			Spacing = 4.0
		};
		stackPanel.Children.Add(_devicePanel);
		border.Child = stackPanel;
		left.Children.Add(border);
	}

	private void BuildControls(StackPanel left)
	{
		Border border = new Border
		{
			CornerRadius = new CornerRadius(12.0),
			Padding = new Thickness(16.0),
			Background = CBg
		};
		_cards.Add(border);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 16.0
		};
		stackPanel.Children.Add(L("controls", 18.0, bold: true));
		StackPanel stackPanel2 = new StackPanel
		{
			Spacing = 4.0
		};
		StackPanel stackPanel3 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		stackPanel3.Children.Add(new FontIcon
		{
			Glyph = "\ue706",
			FontSize = 14.0,
			Foreground = Acc
		});
		stackPanel3.Children.Add(L("brightness", 13.0));
		_brightVal = new TextBlock
		{
			Text = "80%",
			FontSize = 13.0
		};
		stackPanel3.Children.Add(_brightVal);
		stackPanel2.Children.Add(stackPanel3);
		_brightSlider = new Slider
		{
			Minimum = 0.0,
			Maximum = 100.0,
			Value = _vm.Brightness
		};
		_brightSlider.ValueChanged += delegate(object _, RangeBaseValueChangedEventArgs e)
		{
			_vm.SetBrightnessCommand.Execute((int)e.NewValue);
		};
		stackPanel2.Children.Add(_brightSlider);
		stackPanel.Children.Add(stackPanel2);
		StackPanel stackPanel4 = new StackPanel
		{
			Spacing = 4.0
		};
		StackPanel stackPanel5 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		stackPanel5.Children.Add(new FontIcon
		{
			Glyph = "\ue767",
			FontSize = 14.0,
			Foreground = Acc
		});
		stackPanel5.Children.Add(L("volume", 13.0));
		_volVal = new TextBlock
		{
			Text = "50%",
			FontSize = 13.0
		};
		stackPanel5.Children.Add(_volVal);
		stackPanel4.Children.Add(stackPanel5);
		_volSlider = new Slider
		{
			Minimum = 0.0,
			Maximum = 100.0,
			Value = _vm.Volume
		};
		_volSlider.ValueChanged += delegate(object _, RangeBaseValueChangedEventArgs e)
		{
			_vm.SetVolumeCommand.Execute((int)e.NewValue);
		};
		stackPanel4.Children.Add(_volSlider);
		stackPanel.Children.Add(stackPanel4);
		StackPanel rP = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		int[] array = new int[4] { 0, 90, 180, 270 };
		int[] array2 = array;
		int[] array3 = array2;
		int[] array4 = array3;
		int[] array5 = array4;
		int[] array6 = array5;
		int[] array7 = array6;
		int[] array8 = array7;
		int[] array9 = array8;
		int[] array10 = array9;
		foreach (int num2 in array10)
		{
			int angle = num2;
			Button button = new Button
			{
				Width = 52.0,
				Height = 36.0,
				CornerRadius = new CornerRadius(6.0),
				Content = new TextBlock
				{
					Text = $"{num2}°",
					FontSize = 13.0,
					HorizontalAlignment = HorizontalAlignment.Center
				},
				Background = ((_vm.Rotation == num2) ? Acc : CBg),
				Foreground = ((_vm.Rotation == num2) ? Wht : Txt)
			};
			button.Click += delegate
			{
				_vm.SetRotationCommand.Execute(angle);
				foreach (Button child in rP.Children)
				{
					TextBlock textBlock = child.Content as TextBlock;
					int num3 = int.Parse(textBlock.Text.TrimEnd('°'));
					child.Background = ((num3 == angle) ? Acc : CBg);
					child.Foreground = ((num3 == angle) ? Wht : Txt);
				}
			};
			rP.Children.Add(button);
		}
		stackPanel.Children.Add(rP);
		StackPanel stackPanel6 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		_startBtn = new Button
		{
			Content = _vm.T("startDrv"),
			Background = Acc,
			Foreground = Wht,
			CornerRadius = new CornerRadius(6.0)
		};
		_startBtn.Click += delegate
		{
			Debug.WriteLine("[SOEYI] Button clicked");
			_vm.StartDriver();
		};
		_stopBtn = new Button
		{
			Content = _vm.T("stopDrv"),
			CornerRadius = new CornerRadius(6.0)
		};
		_stopBtn.Click += delegate
		{
			_vm.StopDriverCommand.Execute(null);
		};
		stackPanel6.Children.Add(_startBtn);
		stackPanel6.Children.Add(_stopBtn);
		stackPanel.Children.Add(stackPanel6);
		border.Child = stackPanel;
		left.Children.Add(border);
	}

	private void BuildMonitor(StackPanel right)
	{
		Border border = new Border
		{
			CornerRadius = new CornerRadius(12.0),
			Padding = new Thickness(16.0),
			Background = CBg
		};
		_cards.Add(border);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 12.0
		};
		stackPanel.Children.Add(L("monitor", 18.0, bold: true));
		Grid grid = new Grid
		{
			ColumnSpacing = 12.0,
			RowSpacing = 12.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		AddHw(grid, 0, 0, "\ue950", _vm.T("cpu"), "cpu", ref _cpuVal);
		AddHw(grid, 0, 1, "\ue7f4", _vm.T("gpu"), "gpu", ref _gpuVal);
		AddHw(grid, 1, 0, "\ue950", _vm.T("ram"), "ram", ref _ramVal);
		AddHw(grid, 1, 1, "\ue774", _vm.T("network"), "network", ref _netVal);
		stackPanel.Children.Add(grid);
		border.Child = stackPanel;
		right.Children.Add(border);
	}

	private void AddHw(Grid g, int r, int c, string icon, string label, string locKey, ref TextBlock val)
	{
		Border border = new Border
		{
			CornerRadius = new CornerRadius(12.0),
			Padding = new Thickness(16.0),
			Background = CBg
		};
		_cards.Add(border);
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(new FontIcon
		{
			Glyph = icon,
			FontSize = 24.0,
			Foreground = Acc
		});
		TextBlock item = new TextBlock
		{
			Text = label,
			FontSize = 12.0,
			Foreground = TSec,
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0),
			Tag = locKey
		};
		stackPanel.Children.Add(item);
		_hwLabels.Add(item);
		val = new TextBlock
		{
			Text = "--",
			FontSize = 28.0,
			FontWeight = FontWeights.Bold,
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
		};
		stackPanel.Children.Add(val);
		border.Child = stackPanel;
		Grid.SetRow(border, r);
		Grid.SetColumn(border, c);
		g.Children.Add(border);
	}

	private void BuildTheme(StackPanel right)
	{
		Border border = new Border
		{
			CornerRadius = new CornerRadius(12.0),
			Padding = new Thickness(16.0),
			Background = CBg
		};
		_cards.Add(border);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 12.0
		};
		stackPanel.Children.Add(L("theme", 18.0, bold: true));
		_themeDropDown = new DropDownButton
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			MinWidth = 120.0
		};
		_currentThemeLabel = new TextBlock
		{
			Text = _vm.CurrentTheme.Name,
			FontSize = 14.0
		};
		_themeDropDown.Content = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			Children = 
			{
				(UIElement)new FontIcon
				{
					Glyph = "\ue771",
					FontSize = 14.0
				},
				(UIElement)_currentThemeLabel
			}
		};
		_themeFlyout = new MenuFlyout();
		RebuildThemeDropdown();
		_themeDropDown.Flyout = _themeFlyout;
		Button button = new Button
		{
			Content = new FontIcon
			{
				Glyph = "\ue70f",
				FontSize = 14.0
			},
			Width = 36.0,
			Height = 36.0,
			CornerRadius = new CornerRadius(6.0)
		};
		button.Click += async delegate
		{
			await ShowThemeEditor();
		};
		Grid grid = new Grid
		{
			ColumnSpacing = 4.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		Grid.SetColumn(_themeDropDown, 0);
		grid.Children.Add(_themeDropDown);
		Grid.SetColumn(button, 1);
		grid.Children.Add(button);
		stackPanel.Children.Add(grid);
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 4.0
		};
		Button button2 = new Button
		{
			Content = "\ud83c\udd95 " + _vm.T("newTheme"),
			FontSize = 12.0,
			CornerRadius = new CornerRadius(6.0),
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		button2.Click += async delegate
		{
			await NewTheme();
		};
		_newThemeBtn = button2;
		Button button3 = new Button
		{
			Content = "✂ " + _vm.T("bgImage"),
			FontSize = 12.0,
			CornerRadius = new CornerRadius(6.0),
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		button3.Click += async delegate
		{
			await CropAndCreateTheme();
		};
		_bgImageBtn = button3;
		Button button4 = new Button
		{
			Content = "\ud83d\udce5 " + _vm.T("import"),
			FontSize = 12.0,
			CornerRadius = new CornerRadius(6.0),
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		button4.Click += async delegate
		{
			await ImportTheme();
		};
		_importBtn = button4;
		Button button5 = new Button
		{
			Content = "\ud83d\udce4 " + _vm.T("export"),
			FontSize = 12.0,
			CornerRadius = new CornerRadius(6.0),
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		Button button6 = new Button
		{
			Content = "\ud83d\udcbe " + _vm.T("saveAs"),
			FontSize = 12.0,
			CornerRadius = new CornerRadius(6.0),
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		button5.Click += delegate
		{
			ExportTheme();
		};
		_exportBtn = button5;
		button6.Click += delegate
		{
			SaveAsTheme();
		};
		_saveAsBtn = button6;
		Button button7 = new Button
		{
			Content = "\ud83d\uddd1 " + _vm.T("delete"),
			FontSize = 12.0,
			CornerRadius = new CornerRadius(6.0),
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		button7.Click += delegate
		{
			DeleteCurrentTheme();
		};
		Button button8 = new Button
		{
			Content = "\ud83d\udcbe " + _vm.T("save"),
			FontSize = 12.0,
			CornerRadius = new CornerRadius(6.0),
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		button8.Click += delegate
		{
			SaveCurrentTheme();
		};
		Button button9 = new Button
		{
			Content = "⭐ " + _vm.T("setDefault"),
			FontSize = 12.0,
			CornerRadius = new CornerRadius(6.0),
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		button9.Click += delegate
		{
			SetDefaultTheme();
		};
		stackPanel2.Children.Add(button9);
		stackPanel2.Children.Add(button2);
		stackPanel2.Children.Add(button3);
		stackPanel2.Children.Add(button8);
		stackPanel2.Children.Add(button6);
		stackPanel2.Children.Add(button7);
		stackPanel2.Children.Add(button4);
		stackPanel2.Children.Add(button5);
		stackPanel.Children.Add(stackPanel2);
		_themePreviewImg = new Image
		{
			Width = 180.0,
			Height = 832.0,
			Stretch = Stretch.Uniform,
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
		};
		stackPanel.Children.Add(_themePreviewImg);
		RefreshThemePreview();
		_previewRefreshTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(800L)
		};
		_previewRefreshTimer.Tick += delegate
		{
			RefreshThemePreview();
		};
		_previewRefreshTimer.Start();
		border.Child = stackPanel;
		right.Children.Add(border);
	}

	private void BuildSettings(StackPanel right)
	{
		Border border = new Border
		{
			CornerRadius = new CornerRadius(12.0),
			Padding = new Thickness(16.0),
			Background = CBg
		};
		_cards.Add(border);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 12.0
		};
		stackPanel.Children.Add(L("settings", 18.0, bold: true));
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		stackPanel2.Children.Add(L("jpeg"));
		_qualityCombo = new ComboBox
		{
			Width = 120.0
		};
		int[] array = new int[8] { 60, 70, 75, 80, 85, 90, 95, 100 };
		int[] array2 = array;
		int[] array3 = array2;
		int[] array4 = array3;
		int[] array5 = array4;
		int[] array6 = array5;
		int[] array7 = array6;
		int[] array8 = array7;
		int[] array9 = array8;
		int[] array10 = array9;
		foreach (int num in array10)
		{
			_qualityCombo.Items.Add(num);
		}
		_qualityCombo.SelectedIndex = 4;
		_qualityCombo.SelectionChanged += delegate
		{
			if (_qualityCombo.SelectedItem is int jpegQuality)
			{
				_vm.JpegQuality = jpegQuality;
			}
		};
		stackPanel2.Children.Add(_qualityCombo);
		stackPanel.Children.Add(stackPanel2);
		StackPanel stackPanel3 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		stackPanel3.Children.Add(L("language"));
		_langCombo = new ComboBox
		{
			Width = 120.0
		};
		_langCombo.Items.Add("zh-CN");
		_langCombo.Items.Add("en-US");
		_langCombo.Items.Add("ja-JP");
		_langCombo.Items.Add("ko-KR");
		_langCombo.SelectedIndex = 0;
		_langCombo.SelectionChanged += delegate
		{
			if (_langCombo.SelectedItem is string selectedLanguage)
			{
				_vm.SelectedLanguage = selectedLanguage;
			}
		};
		stackPanel3.Children.Add(_langCombo);
		stackPanel.Children.Add(stackPanel3);
		StackPanel stackPanel4 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		stackPanel4.Children.Add(L("startMin"));
		_startMinToggle = new ToggleSwitch { VerticalAlignment = VerticalAlignment.Center };
		_startMinToggle.Toggled += delegate
		{
			_vm.StartMinimized = !_vm.StartMinimized;
		};
		stackPanel4.Children.Add(_startMinToggle);
		stackPanel.Children.Add(stackPanel4);
		StackPanel stackPanel5 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		stackPanel5.Children.Add(L("fanControl"));
		ToggleSwitch fcToggle = new ToggleSwitch
		{
			IsOn = _vm.UseFanControl,
			VerticalAlignment = VerticalAlignment.Center
		};
		fcToggle.Toggled += delegate
		{
			_vm.UseFanControl = fcToggle.IsOn;
		};
		stackPanel5.Children.Add(fcToggle);
		stackPanel.Children.Add(stackPanel5);
		StackPanel stackPanel6 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		stackPanel6.Children.Add(new FontIcon
		{
			Glyph = "\ue708",
			FontSize = 16.0,
			Foreground = Acc
		});
		stackPanel6.Children.Add(L("darkMode"));
		_darkToggle = new ToggleSwitch
		{
			IsOn = _vm.IsDarkTheme,
			VerticalAlignment = VerticalAlignment.Center
		};
		_darkToggle.Toggled += delegate
		{
			if (!_settingThemeProgrammatically)
			{
				_vm.ToggleDarkModeCommand.Execute(null);
			}
		};
		stackPanel6.Children.Add(_darkToggle);
		stackPanel.Children.Add(stackPanel6);
		StackPanel stackPanel7 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		stackPanel7.Children.Add(new FontIcon
		{
			Glyph = "\ue7a7",
			FontSize = 16.0,
			Foreground = Acc
		});
		stackPanel7.Children.Add(L("followSystemTheme"));
		ToggleSwitch sysToggle = new ToggleSwitch
		{
			IsOn = _vm.UseSystemTheme,
			VerticalAlignment = VerticalAlignment.Center
		};
		sysToggle.Toggled += delegate
		{
			_vm.UseSystemTheme = sysToggle.IsOn;
			if (sysToggle.IsOn)
			{
				ApplySystemTheme();
			}
		};
		stackPanel7.Children.Add(sysToggle);
		stackPanel.Children.Add(stackPanel7);
		StackPanel stackPanel8 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		stackPanel8.Children.Add(new FontIcon
		{
			Glyph = "\ue703",
			FontSize = 16.0,
			Foreground = Acc
		});
		stackPanel8.Children.Add(L("autoStart"));
		ToggleSwitch autoToggle = new ToggleSwitch
		{
			IsOn = _vm.StartWithWindows,
			VerticalAlignment = VerticalAlignment.Center
		};
		autoToggle.Toggled += delegate
		{
			_vm.StartWithWindows = autoToggle.IsOn;
		};
		stackPanel8.Children.Add(autoToggle);
		stackPanel.Children.Add(stackPanel8);
		_saveBtn = new Button
		{
			Content = _vm.T("save"),
			Background = Acc,
			Foreground = Wht,
			CornerRadius = new CornerRadius(6.0),
			HorizontalAlignment = HorizontalAlignment.Left
		};
		_saveBtn.Click += delegate
		{
			_vm.SaveConfigCommand.Execute(null);
		};
		stackPanel.Children.Add(_saveBtn);
		border.Child = stackPanel;
		right.Children.Add(border);
	}

	private static SolidColorBrush ParseHex(string h)
	{
		if (h.StartsWith("#"))
		{
			h = h.Substring(1);
		}
		return new SolidColorBrush(Color.FromArgb(byte.MaxValue, Convert.ToByte(h.Substring(0, 2), 16), Convert.ToByte(h.Substring(2, 2), 16), Convert.ToByte(h.Substring(4, 2), 16)));
	}

	private void ApplyLocalization()
	{
		string selectedLanguage = _vm.SelectedLanguage;
		foreach (var item3 in _loc)
		{
			TextBlock item = item3.t;
			string item2 = item3.key;
			item.Text = _vm.T(item2);
		}
		_badgeText.Text = _vm.T("ready");
		_footerText.Text = _vm.T("ready");
		_deviceCountBlock.Text = _vm.T("devices") + ": " + _vm.AttachedDevices.Count;
		_driverStatusBlock.Text = (_vm.IsDriverRunning ? _vm.T("running") : _vm.T("stopped"));
		if (_startBtn != null)
		{
			_startBtn.Content = _vm.T("startDrv");
		}
		if (_stopBtn != null)
		{
			_stopBtn.Content = _vm.T("stopDrv");
		}
		if (_saveBtn != null)
		{
			_saveBtn.Content = _vm.T("save");
		}
		if (_dashItem != null)
		{
			_dashItem.Content = _vm.T("dashboard");
		}
		if (_themeItem != null)
		{
			_themeItem.Content = _vm.T("theme");
		}
		if (_settingsItem != null)
		{
			_settingsItem.Content = _vm.T("settings");
		}
		if (_newThemeBtn != null) _newThemeBtn.Content = "🆕 " + _vm.T("newTheme");
		if (_bgImageBtn != null) _bgImageBtn.Content = "✂ " + _vm.T("bgImage");
		if (_importBtn != null) _importBtn.Content = "📥 " + _vm.T("import");
		if (_exportBtn != null) _exportBtn.Content = "📤 " + _vm.T("export");
		if (_saveAsBtn != null) _saveAsBtn.Content = "💾 " + _vm.T("saveAs");
		if (_saveBtn2 != null) _saveBtn2.Content = "💾 " + _vm.T("save");
		if (_delBtn != null) _delBtn.Content = "🗑 " + _vm.T("delete");
		foreach (TextBlock hwLabel in _hwLabels)
		{
			if (hwLabel.Tag is string key)
			{
				hwLabel.Text = _vm.T(key);
			}
		}
		RefreshDevicePanel();
	}

	private void RefreshDevicePanel()
	{
		_devicePanel.Children.Clear();
		_deviceCountBlock.Text = _vm.T("devices") + ": " + _vm.AttachedDevices.Count;
		if (_vm.AttachedDevices.Count == 0)
		{
			_devicePanel.Children.Add(new TextBlock
			{
				Text = _vm.T("noDevice"),
				HorizontalAlignment = HorizontalAlignment.Center,
				Opacity = 0.6,
				Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
			});
			return;
		}
		foreach (DeviceInfo attachedDevice in _vm.AttachedDevices)
		{
			Border border = new Border
			{
				CornerRadius = new CornerRadius(8.0),
				Padding = new Thickness(12.0),
				Margin = new Thickness(0.0, 4.0, 0.0, 0.0),
				Background = CBg
			};
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 4.0
			};
			StackPanel stackPanel2 = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 8.0
			};
			stackPanel2.Children.Add(new FontIcon
			{
				Glyph = "\ue7f4",
				FontSize = 16.0,
				Foreground = Acc
			});
			stackPanel2.Children.Add(new TextBlock
			{
				Text = attachedDevice.Model,
				FontWeight = FontWeights.SemiBold,
				FontSize = 14.0
			});
			Border border2 = new Border
			{
				CornerRadius = new CornerRadius(8.0),
				Padding = new Thickness(8.0, 2.0, 8.0, 2.0),
				Background = Acc
			};
			border2.Child = new TextBlock
			{
				Text = (attachedDevice.IsAttached ? _vm.T("running") : _vm.T("stopped")),
				FontSize = 11.0,
				Foreground = Wht
			};
			stackPanel2.Children.Add(border2);
			stackPanel.Children.Add(stackPanel2);
			StackPanel stackPanel3 = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 12.0,
				Margin = new Thickness(4.0, 0.0, 0.0, 0.0)
			};
			stackPanel3.Children.Add(new TextBlock
			{
				Text = _vm.T("serialNumber") + ": " + attachedDevice.SerialNumber,
				FontSize = 12.0,
				Foreground = TSec
			});
			stackPanel3.Children.Add(new TextBlock
			{
				Text = attachedDevice.CurrentResolution.ToString(),
				FontSize = 12.0,
				Foreground = TSec
			});
			stackPanel3.Children.Add(new TextBlock
			{
				Text = _vm.T("handle") + ": 0x" + attachedDevice.Handle.ToString("X"),
				FontSize = 12.0,
				Foreground = TSec
			});
			stackPanel.Children.Add(stackPanel3);
			border.Child = stackPanel;
			_devicePanel.Children.Add(border);
		}
	}

	private void UpdateMonitorCards()
	{
		HwSnapshot hwSnapshot = _vm.HwSnapshot;
		if (hwSnapshot.CpuUsage > 0f)
		{
			_lastHw = hwSnapshot;
		}
		else
		{
			hwSnapshot = _lastHw;
		}
		_cpuVal.Text = $"{hwSnapshot.CpuUsage:F0}%  {hwSnapshot.CpuTemp:F0}°C";
		_gpuVal.Text = $"{hwSnapshot.GpuUsage:F0}%  {hwSnapshot.GpuTemp:F0}°C";
		_ramVal.Text = $"{hwSnapshot.RamUsageMb / 1024f:F1}/{(float)hwSnapshot.TotalRamMb / 1024f:F0}G";
		_netVal.Text = "↓" + FmtSpeed(hwSnapshot.NetDownKbps) + " ↑" + FmtSpeed(hwSnapshot.NetUpKbps);
	}

	private void BindViewModel()
	{
		_vm.PropertyChanged += delegate(object? _, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "SelectedLanguage")
			{
				base.DispatcherQueue.TryEnqueue(ApplyLocalization);
			}
			else
			{
				base.DispatcherQueue.TryEnqueue(delegate
				{
					switch (e.PropertyName)
					{
					case "StatusMessage":
						_badgeText.Text = _vm.StatusMessage;
						_footerText.Text = _vm.StatusMessage;
						break;
					case "IsDriverRunning":
						_driverStatusBlock.Text = (_vm.IsDriverRunning ? _vm.T("running") : _vm.T("stopped"));
						break;
					case "Brightness":
						_brightVal.Text = $"{_vm.Brightness}%";
						_brightSlider.Value = _vm.Brightness;
						break;
					case "Volume":
						_volVal.Text = $"{_vm.Volume}%";
						_volSlider.Value = _vm.Volume;
						break;
					case "IsDarkTheme":
						_settingThemeProgrammatically = true;
						_darkToggle.IsOn = _vm.IsDarkTheme;
						_settingThemeProgrammatically = false;
						SetColorScheme(_vm.IsDarkTheme);
						break;
					case "StartMinimized":
						_startMinToggle.IsOn = _vm.StartMinimized;
						break;
					case "CurrentTheme":
						_currentThemeLabel.Text = _vm.CurrentTheme.Name;
						RefreshThemePreview();
						break;
					case "HwSnapshot":
						UpdateMonitorCards();
						break;
					}
				});
			}
		};
		_vm.AttachedDevices.CollectionChanged += delegate
		{
			base.DispatcherQueue.TryEnqueue(RefreshDevicePanel);
		};
	}

	private async Task AddNewIndicator(SoeyiTheme theme, List<SettingElement> existingSettings)
	{
		string[] sourceKeys = new string[15]
		{
			"CpuUsage", "CPUT", "GpuUsage", "GPUT", "MemoryUsage", "MemoryFrequency", "CpuPower", "GpuPower", "WeatherInfo", "NightWeather",
			"HeightWeather", "LowWeather", "CurrentTimeShut", "CurrentDate", "CurrentWeek"
		};
		Dictionary<string, string> defaultUnits = new Dictionary<string, string>
		{
			["CpuUsage"] = "%",
			["CPUT"] = "°C",
			["GpuUsage"] = "%",
			["GPUT"] = "°C",
			["MemoryUsage"] = "G",
			["MemoryFrequency"] = "MHz",
			["CpuPower"] = "W",
			["GpuPower"] = "W"
		};
		ComboBox combo = new ComboBox
		{
			Width = 220.0,
			FontSize = 13.0
		};
		string[] array = sourceKeys;
		string[] array2 = array;
		string[] array3 = array2;
		string[] array4 = array3;
		string[] array5 = array4;
		string[] array6 = array5;
		foreach (string key in array6)
		{
			string dk = ThemeRenderer.GetDataDisplayName(key);
			combo.Items.Add(new ComboBoxItem
			{
				Content = _vm.T(dk),
				Tag = key
			});
		}
		combo.SelectedIndex = 0;
		ContentDialog picker = new ContentDialog
		{
			Title = _vm.T("addIndicator"),
			Content = new StackPanel
			{
				Spacing = 8.0,
				Children = { (UIElement)combo }
			},
			PrimaryButtonText = _vm.T("editorSave"),
			SecondaryButtonText = _vm.T("editorCancel"),
			DefaultButton = ContentDialogButton.Primary,
			XamlRoot = base.XamlRoot
		};
		if (await picker.ShowAsync() != ContentDialogResult.Primary)
		{
			return;
		}
		string selectedKey = (combo.SelectedItem as ComboBoxItem)?.Tag as string;
		if (string.IsNullOrEmpty(selectedKey))
		{
			return;
		}
		double newX = 100.0;
		double newY = 100.0;
		if (existingSettings.Count > 0)
		{
			SettingElement last = existingSettings.OrderBy((SettingElement settingElement) => settingElement.Y).Last();
			newY = last.Y + 80.0;
			if (newY > theme.Height - 60.0)
			{
				newY = 100.0;
			}
			newX = last.X;
		}
		defaultUnits.TryGetValue(selectedKey, out string defUnit);
		SettingElement elem = new SettingElement
		{
			Type = "Text",
			X = newX,
			Y = newY,
			Z = 0,
			FontSize = 25.0,
			Data = selectedKey,
			Unit = (string.IsNullOrEmpty(defUnit) ? null : defUnit),
			Foreground = "#FFFFFF",
			Visible = true
		};
		existingSettings.Add(elem);
		string settingPath = Path.Combine(theme.FolderPath, "Setting.txt");
		try
		{
			List<string> lines = new List<string>();
			foreach (SettingElement e in existingSettings.OrderBy((SettingElement s) => s.Z))
			{
				List<string> parts = new List<string> { "Text:" };
				parts.Add($"x@{e.X}");
				parts.Add($"y@{e.Y}");
				parts.Add($"z@{e.Z}");
				if (e.MaxWidth.HasValue)
				{
					parts.Add($"maxwidth@{e.MaxWidth}");
				}
				if (e.MaxHeight.HasValue)
				{
					parts.Add($"maxheight@{e.MaxHeight}");
				}
				if (!e.Visible)
				{
					parts.Add("visible@false");
				}
				if (e.Centered)
				{
					parts.Add("center@true");
					if (!e.LabelVisible)
					{
						parts.Add("labelVisible@false");
					}
				}
				if (e.FontSize.HasValue)
				{
					parts.Add($"FontSize@{e.FontSize}");
				}
				if (e.LabelFontSize.HasValue)
				{
					parts.Add($"LabelFontSize@{e.LabelFontSize}");
				}
				if (e.FontFamily != null)
				{
					parts.Add("FontFamily@" + e.FontFamily);
				}
				if (e.Foreground != null)
				{
					parts.Add("Foreground@" + e.Foreground);
				}
				if (e.Data != null)
				{
					parts.Add("data@" + e.Data);
				}
				if (e.Unit != null)
				{
					parts.Add("unit@" + e.Unit);
				}
				lines.Add(string.Join(",", parts));
			}
			File.WriteAllText(settingPath, string.Join("\n", lines));
		}
		catch (Exception ex)
		{
			Debug.WriteLine("AddIndicator failed: " + ex.Message);
		}
	}

	private async Task ShowThemeEditor()
	{
		ContentDialog editorDlg = null;
		SoeyiTheme theme = _vm.CurrentTheme;
		if (theme.FolderPath == null)
		{
			return;
		}
		List<SettingElement> settings = ThemeService.GetThemeSettings(theme);
		if (settings.Count == 0)
		{
			return;
		}
		ScrollViewer scroll = new ScrollViewer
		{
			MaxHeight = 620.0
		};
		StackPanel panel = new StackPanel
		{
			Spacing = 16.0,
			Padding = new Thickness(4.0)
		};
		StackPanel heading = new StackPanel
		{
			Spacing = 2.0
		};
		Grid dragHandle = new Grid
		{
			Height = 28.0,
			Background = new SolidColorBrush(Colors.Transparent)
		};
		dragHandle.Children.Add(new TextBlock
		{
			Text = "⋮⋮ " + _vm.T("editorTitle") + ": " + theme.Name,
			FontWeight = FontWeights.SemiBold,
			FontSize = 15.0,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(4.0, 0.0, 0.0, 0.0)
		});
		Point _dragStart = default(Point);
		bool _dragging = false;
		dragHandle.PointerPressed += delegate(object s, PointerRoutedEventArgs e)
		{
			_dragStart = e.GetCurrentPoint(null).Position;
			_dragging = true;
			dragHandle.CapturePointer(e.Pointer);
		};
		dragHandle.PointerMoved += delegate(object s, PointerRoutedEventArgs e)
		{
			if (_dragging)
			{
				Point position = e.GetCurrentPoint(null).Position;
				double num = position.X - _dragStart.X;
				double num2 = position.Y - _dragStart.Y;
				_dragStart = position;
				TranslateTransform translateTransform = editorDlg?.RenderTransform as TranslateTransform;
				if (translateTransform == null && editorDlg != null)
				{
					Transform transform = (editorDlg.RenderTransform = new TranslateTransform());
					Transform transform3 = transform;
					Transform transform4 = transform3;
					Transform transform5 = transform4;
					Transform transform6 = transform5;
					translateTransform = (TranslateTransform)transform6;
				}
				translateTransform.X += num;
				translateTransform.Y += num2;
			}
		};
		dragHandle.PointerReleased += delegate(object s, PointerRoutedEventArgs e)
		{
			_dragging = false;
			dragHandle.ReleasePointerCapture(e.Pointer);
		};
		panel.Children.Add(dragHandle);
		heading.Children.Add(new TextBlock
		{
			Text = _vm.T("editorTitle") + ": " + theme.Name,
			FontWeight = FontWeights.SemiBold,
			FontSize = 16.0
		});
		heading.Children.Add(new TextBlock
		{
			Text = _vm.T("editorThemeSize") + ": " + theme.Width + "×" + theme.Height,
			FontSize = 12.0,
			Opacity = 0.6
		});
		panel.Children.Add(heading);
		Button addBtn = new Button
		{
			Content = "➕ " + _vm.T("addIndicator"),
			FontSize = 12.0,
			CornerRadius = new CornerRadius(6.0),
			Height = 32.0,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		addBtn.Click += async delegate
		{
			editorDlg?.Hide();
			await AddNewIndicator(theme, settings);
			await ShowThemeEditor();
		};
		panel.Children.Add(addBtn);
		List<(SettingElement Elem, Slider XSlider, TextBlock XLabel, Slider YSlider, TextBlock YLabel, NumberBox? FsBox, NumberBox? LfsBox, ToggleSwitch VisChk)> rows = new List<(SettingElement, Slider, TextBlock, Slider, TextBlock, NumberBox, NumberBox, ToggleSwitch)>();
		foreach (SettingElement elem in settings.OrderBy((SettingElement e) => e.Z))
		{
			if (elem.Type != "Text" && elem.Type != "BorderLine")
			{
				continue;
			}
			Border card = new Border
			{
				CornerRadius = new CornerRadius(8.0),
				Padding = new Thickness(12.0),
				Background = CBg
			};
			StackPanel sp = new StackPanel
			{
				Spacing = 6.0
			};
			StackPanel hdr = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6.0
			};
			hdr.Children.Add(new TextBlock
			{
				Text = "▸",
				Foreground = Acc,
				FontSize = 13.0,
				VerticalAlignment = VerticalAlignment.Center
			});
			string dsKey = ThemeRenderer.GetDataDisplayName(elem.Data);
			string dsName = _vm.T(dsKey);
			hdr.Children.Add(new TextBlock
			{
				Text = ((elem.Type == "Text") ? _vm.T("editorText") : _vm.T("editorBar")) + " · " + dsName,
				FontWeight = FontWeights.SemiBold,
				FontSize = 13.0
			});
			Button upBtn = new Button
			{
				Content = "▲",
				FontSize = 10.0,
				Width = 24.0,
				Height = 24.0,
				Padding = new Thickness(0.0),
				CornerRadius = new CornerRadius(4.0),
				Background = new SolidColorBrush(Colors.Transparent),
				Foreground = new SolidColorBrush(Colors.Gray)
			};
			Button downBtn = new Button
			{
				Content = "▼",
				FontSize = 10.0,
				Width = 24.0,
				Height = 24.0,
				Padding = new Thickness(0.0),
				CornerRadius = new CornerRadius(4.0),
				Background = new SolidColorBrush(Colors.Transparent),
				Foreground = new SolidColorBrush(Colors.Gray)
			};
			SettingElement capElem = elem;
			upBtn.Click += delegate
			{
				List<SettingElement> list = settings.OrderBy((SettingElement s) => s.Z).ToList();
				for (int num = 0; num < list.Count; num++)
				{
					list[num].Z = num;
				}
				int num2 = list.IndexOf(capElem);
				if (num2 > 0)
				{
					SettingElement settingElement = list[num2];
					SettingElement settingElement2 = list[num2 - 1];
					int z = list[num2 - 1].Z;
					int z2 = list[num2].Z;
					settingElement.Z = z;
					settingElement2.Z = z2;
					settingElement = list[num2];
					SettingElement settingElement3 = list[num2 - 1];
					double y = list[num2 - 1].Y;
					double y2 = list[num2].Y;
					settingElement.Y = y;
					settingElement3.Y = y2;
					settingElement = list[num2];
					SettingElement settingElement4 = list[num2 - 1];
					y2 = list[num2 - 1].X;
					y = list[num2].X;
					settingElement.X = y2;
					settingElement4.X = y;
				}
				WriteSettingsLive(theme, settings);
				RefreshThemePreview();
				editorDlg?.Hide();
				ShowThemeEditor();
			};
			_vm.DeviceSvc?.SendFrame();
			downBtn.Click += delegate
			{
				List<SettingElement> list = settings.OrderBy((SettingElement s) => s.Z).ToList();
				for (int num = 0; num < list.Count; num++)
				{
					list[num].Z = num;
				}
				int num2 = list.IndexOf(capElem);
				if (num2 < list.Count - 1)
				{
					SettingElement settingElement = list[num2];
					SettingElement settingElement2 = list[num2 + 1];
					int z = list[num2 + 1].Z;
					int z2 = list[num2].Z;
					settingElement.Z = z;
					settingElement2.Z = z2;
					settingElement = list[num2];
					SettingElement settingElement3 = list[num2 + 1];
					double y = list[num2 + 1].Y;
					double y2 = list[num2].Y;
					settingElement.Y = y;
					settingElement3.Y = y2;
					settingElement = list[num2];
					SettingElement settingElement4 = list[num2 + 1];
					y2 = list[num2 + 1].X;
					y = list[num2].X;
					settingElement.X = y2;
					settingElement4.X = y;
				}
				WriteSettingsLive(theme, settings);
				RefreshThemePreview();
				editorDlg?.Hide();
				ShowThemeEditor();
			};
			_vm.DeviceSvc?.SendFrame();
			hdr.Children.Add(upBtn);
			hdr.Children.Add(downBtn);
			Button delBtn = new Button
			{
				Content = "✕",
				FontSize = 11.0,
				Width = 24.0,
				Height = 24.0,
				Padding = new Thickness(0.0),
				CornerRadius = new CornerRadius(4.0),
				HorizontalAlignment = HorizontalAlignment.Right,
				Background = new SolidColorBrush(Colors.Transparent),
				Foreground = new SolidColorBrush(Colors.IndianRed)
			};
			SettingElement capturedElem = elem;
			delBtn.Click += delegate
			{
				settings.Remove(capturedElem);
				WriteSettingsLive(theme, settings);
				_vm.DeviceSvc?.SendFrame();
				RefreshThemePreview();
				editorDlg?.Hide();
				ShowThemeEditor();
			};
			hdr.Children.Add(delBtn);
			sp.Children.Add(hdr);
			StackPanel xRow = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 10.0,
				VerticalAlignment = VerticalAlignment.Center
			};
			TextBlock xTb = new TextBlock
			{
				Text = "X",
				Width = 14.0,
				FontSize = 12.0,
				VerticalAlignment = VerticalAlignment.Center
			};
			Slider xSlider = new Slider
			{
				Minimum = -100.0,
				Maximum = (int)theme.Width + 200,
				Value = elem.X,
				Width = 300.0
			};
			TextBlock xVal = new TextBlock
			{
				Text = $"{elem.X:F0}",
				Width = 40.0,
				FontSize = 12.0,
				VerticalAlignment = VerticalAlignment.Center,
				TextAlignment = TextAlignment.Right
			};
			xSlider.ValueChanged += delegate
			{
				elem.Centered = false;
				elem.X = xSlider.Value;
				xVal.Text = $"{xSlider.Value:F0}";
				WriteSettingsLive(theme, settings);
				_vm.DeviceSvc?.SendFrame();
				RefreshThemePreview();
			};
			xRow.Children.Add(xTb);
			xRow.Children.Add(xSlider);
			xRow.Children.Add(xVal);
			sp.Children.Add(xRow);
			StackPanel yRow = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 10.0,
				VerticalAlignment = VerticalAlignment.Center
			};
			TextBlock yTb = new TextBlock
			{
				Text = "Y",
				Width = 14.0,
				FontSize = 12.0,
				VerticalAlignment = VerticalAlignment.Center
			};
			Slider ySlider = new Slider
			{
				Minimum = -100.0,
				Maximum = (int)theme.Height + 200,
				Value = elem.Y,
				Width = 300.0
			};
			TextBlock yVal = new TextBlock
			{
				Text = $"{elem.Y:F0}",
				Width = 40.0,
				FontSize = 12.0,
				VerticalAlignment = VerticalAlignment.Center,
				TextAlignment = TextAlignment.Right
			};
			ySlider.ValueChanged += delegate
			{
				elem.Y = ySlider.Value;
				yVal.Text = $"{ySlider.Value:F0}";
				WriteSettingsLive(theme, settings);
				_vm.DeviceSvc?.SendFrame();
				RefreshThemePreview();
			};
			yRow.Children.Add(yTb);
			yRow.Children.Add(ySlider);
			yRow.Children.Add(yVal);
			sp.Children.Add(yRow);
			NumberBox fsb = null;
			NumberBox lfsb = null;
			ToggleSwitch visChk = null;
			if (elem.Type == "Text")
			{
				StackPanel fsRow = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Spacing = 0.0,
					VerticalAlignment = VerticalAlignment.Center
				};
				fsRow.Children.Add(new TextBlock
				{
					Text = _vm.T("editorFs"),
					Width = 50.0,
					FontSize = 12.0,
					VerticalAlignment = VerticalAlignment.Center
				});
				fsb = new NumberBox
				{
					Value = (elem.FontSize ?? 25.0),
					Minimum = 4.0,
					Maximum = 200.0,
					Width = 80.0,
					FontSize = 12.0,
					IsTabStop = true
				};
				fsb.ValueChanged += delegate
				{
					elem.FontSize = fsb.Value;
					WriteSettingsLive(theme, settings);
					_vm.DeviceSvc?.SendFrame();
					RefreshThemePreview();
				};
				fsRow.Children.Add(fsb);
				fsRow.Children.Add(new TextBlock
				{
					Text = _vm.T("editorLfs"),
					Width = 50.0,
					FontSize = 12.0,
					VerticalAlignment = VerticalAlignment.Center,
					Margin = new Thickness(8.0, 0.0, 0.0, 0.0)
				});
				lfsb = new NumberBox
				{
					Value = (elem.LabelFontSize ?? 12.0),
					Minimum = 4.0,
					Maximum = 100.0,
					Width = 80.0,
					FontSize = 12.0,
					IsTabStop = true
				};
				lfsb.ValueChanged += delegate
				{
					elem.LabelFontSize = lfsb.Value;
					WriteSettingsLive(theme, settings);
					_vm.DeviceSvc?.SendFrame();
					RefreshThemePreview();
				};
				fsRow.Children.Add(lfsb);
				StackPanel visRow = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Spacing = 10.0,
					Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
				};
				visChk = new ToggleSwitch
				{
					IsOn = elem.Visible,
					FontSize = 11.0,
					OffContent = "",
					OnContent = "",
					MinWidth = 0.0
				};
				visChk.Toggled += delegate
				{
					elem.Visible = visChk.IsOn;
					WriteSettingsLive(theme, settings);
					_vm.DeviceSvc?.SendFrame();
					RefreshThemePreview();
				};
				ToggleSwitch centerChk = new ToggleSwitch
				{
					IsOn = elem.Centered,
					FontSize = 11.0,
					OffContent = "",
					OnContent = "",
					MinWidth = 0.0
				};
				centerChk.Toggled += delegate
				{
					elem.Centered = centerChk.IsOn;
					WriteSettingsLive(theme, settings);
					_vm.DeviceSvc?.SendFrame();
					RefreshThemePreview();
				};
				ToggleSwitch lblVisChk = new ToggleSwitch
				{
					IsOn = elem.LabelVisible,
					FontSize = 11.0,
					OffContent = "",
					OnContent = "",
					MinWidth = 0.0
				};
				lblVisChk.Toggled += delegate
				{
					elem.LabelVisible = lblVisChk.IsOn;
					WriteSettingsLive(theme, settings);
					_vm.DeviceSvc?.SendFrame();
					RefreshThemePreview();
				};
				visRow.Children.Add(new TextBlock
				{
					Text = _vm.T("editorVisible"),
					FontSize = 12.0,
					VerticalAlignment = VerticalAlignment.Center
				});
				visRow.Children.Add(visChk);
				visRow.Children.Add(new TextBlock
				{
					Text = _vm.T("editorCenter"),
					FontSize = 12.0,
					VerticalAlignment = VerticalAlignment.Center
				});
				visRow.Children.Add(centerChk);
				visRow.Children.Add(new TextBlock
				{
					Text = _vm.T("editorLabelVis"),
					FontSize = 12.0,
					VerticalAlignment = VerticalAlignment.Center
				});
				visRow.Children.Add(lblVisChk);
				sp.Children.Add(visRow);
				sp.Children.Add(fsRow);
			}
			card.Child = sp;
			panel.Children.Add(card);
			rows.Add((elem, xSlider, xVal, ySlider, yVal, fsb, lfsb, visChk));
		}
		scroll.Content = panel;
		editorDlg = new ContentDialog
		{
			Title = null,
			MinWidth = 900.0,
			MaxWidth = 1200.0,
			Content = scroll,
			PrimaryButtonText = _vm.T("editorSave"),
			SecondaryButtonText = _vm.T("editorCancel"),
			DefaultButton = ContentDialogButton.Primary,
			XamlRoot = base.XamlRoot,
			RequestedTheme = ((!_vm.IsDarkTheme) ? ElementTheme.Light : ElementTheme.Dark)
		};
		if (await editorDlg.ShowAsync() != ContentDialogResult.Primary)
		{
			return;
		}
		foreach (var item in rows)
		{
			SettingElement elem2 = item.Elem;
			Slider xs = item.XSlider;
			Slider ys = item.YSlider;
			NumberBox fsb2 = item.FsBox;
			elem2.X = xs.Value;
			elem2.Y = ys.Value;
			if (fsb2 != null)
			{
				elem2.FontSize = fsb2.Value;
				if (item.LfsBox != null)
				{
					elem2.LabelFontSize = item.LfsBox.Value;
				}
			}
			elem2.Visible = item.VisChk.IsOn;
		}
		List<string> lines = new List<string>();
		string settingPath = Path.Combine(theme.FolderPath, "Setting.txt");
		foreach (SettingElement elem3 in settings)
		{
			List<string> parts = new List<string> { elem3.Type + ":" };
			if (elem3.Type == "Text")
			{
				parts.Add($"x@{elem3.X}");
				parts.Add($"y@{elem3.Y}");
				parts.Add($"z@{elem3.Z}");
				if (elem3.MaxHeight.HasValue)
				{
					parts.Add($"maxheight@{elem3.MaxHeight}");
				}
				if (elem3.MaxWidth.HasValue)
				{
					parts.Add($"maxwidth@{elem3.MaxWidth}");
				}
				if (elem3.FontSize.HasValue)
				{
					parts.Add($"FontSize@{elem3.FontSize}");
				}
				if (elem3.LabelFontSize.HasValue)
				{
					parts.Add($"LabelFontSize@{elem3.LabelFontSize}");
				}
				if (elem3.Centered)
				{
					parts.Add("center@true");
				}
				if (!elem3.LabelVisible)
				{
					parts.Add("labelVisible@false");
				}
				if (elem3.FontFamily != null)
				{
					parts.Add("FontFamily@" + elem3.FontFamily);
				}
				if (elem3.Foreground != null)
				{
					parts.Add("Foreground@" + elem3.Foreground);
				}
				if (elem3.Title != null)
				{
					parts.Add("Title@" + elem3.Title);
				}
				if (elem3.Data != null)
				{
					parts.Add("data@" + elem3.Data);
				}
				if (elem3.Unit != null)
				{
					parts.Add("unit@" + elem3.Unit);
				}
			}
			else
			{
				if (!(elem3.Type == "BorderLine"))
				{
					string orig = TryGetOriginalLine(settingPath, elem3.Type);
					lines.Add(orig ?? (elem3.Type + ":"));
					continue;
				}
				parts.Add($"x@{elem3.X}");
				parts.Add($"y@{elem3.Y}");
				parts.Add($"z@{elem3.Z}");
				if (elem3.MaxHeight.HasValue)
				{
					parts.Add($"maxheight@{elem3.MaxHeight}");
				}
				if (elem3.MaxWidth.HasValue)
				{
					parts.Add($"maxwidth@{elem3.MaxWidth}");
				}
				if (elem3.Fill != null)
				{
					parts.Add("Fill@" + elem3.Fill);
				}
				if (elem3.Data != null)
				{
					parts.Add("data@" + elem3.Data);
				}
			}
			lines.Add(string.Join(",", parts));
		}
		File.WriteAllText(settingPath, string.Join("\n", lines));
		_vm.SelectThemeCommand.Execute(theme.Name);
	}

	private static string? TryGetOriginalLine(string path, string type)
	{
		try
		{
			string[] array = File.ReadAllLines(path);
			string[] array2 = array;
			string[] array3 = array2;
			string[] array4 = array3;
			string[] array5 = array4;
			string[] array6 = array5;
			string[] array7 = array6;
			string[] array8 = array7;
			string[] array9 = array8;
			string[] array10 = array9;
			foreach (string text in array10)
			{
				if (text.Trim().StartsWith(type + ":"))
				{
					return text.Trim();
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("[Soeyi] WriteSettingsLive failed: " + ex.Message);
		}
		return null;
	}

	[DllImport("comdlg32.dll", CharSet = CharSet.Auto)]
	private static extern bool GetOpenFileName(ref OpenFileName ofn);

	[DllImport("comdlg32.dll", CharSet = CharSet.Auto)]
	private static extern bool GetSaveFileName(ref OpenFileName ofn);

	private static string? ShowOpenFileDialog(string title, string filter)
	{
		OpenFileName ofn = default(OpenFileName);
		ofn.lStructSize = Marshal.SizeOf(ofn);
		ofn.lpstrFilter = filter.Replace("|", "\0") + "\0\0";
		ofn.lpstrFile = new string('\0', 512);
		ofn.nMaxFile = 512;
		ofn.lpstrTitle = title;
		ofn.Flags = 528384;
		Window window = (Application.Current as App)?._w;
		if (window != null)
		{
			ofn.hwndOwner = WindowNative.GetWindowHandle(window);
		}
		if (GetOpenFileName(ref ofn))
		{
			return ofn.lpstrFile?.TrimEnd('\0');
		}
		return null;
	}

	private static string? ShowSaveFileDialog(string title, string filter, string defaultName)
	{
		OpenFileName ofn = default(OpenFileName);
		ofn.lStructSize = Marshal.SizeOf(ofn);
		ofn.lpstrFilter = filter.Replace("|", "\0") + "\0\0";
		ofn.lpstrFile = (defaultName ?? "").PadRight(512, '\0');
		ofn.nMaxFile = 512;
		ofn.lpstrTitle = title;
		ofn.lpstrDefExt = "json";
		ofn.Flags = 524290;
		Window window = (Application.Current as App)?._w;
		if (window != null)
		{
			ofn.hwndOwner = WindowNative.GetWindowHandle(window);
		}
		if (GetSaveFileName(ref ofn))
		{
			return ofn.lpstrFile?.TrimEnd('\0');
		}
		return null;
	}

	private void WriteSettingsLive(SoeyiTheme theme, List<SettingElement> settings)
	{
		try
		{
			if (string.IsNullOrEmpty(theme.FolderPath))
			{
				return;
			}
			string path = Path.Combine(theme.FolderPath, "Setting.txt");
			List<string> list = new List<string>();
			foreach (SettingElement item in settings.OrderBy((SettingElement s) => s.Z))
			{
				if (item.Type == "Text")
				{
					List<string> list2 = new List<string> { "Text:" };
					list2.Add($"x@{item.X}");
					list2.Add($"y@{item.Y}");
					list2.Add($"z@{item.Z}");
					if (item.MaxWidth.HasValue)
					{
						list2.Add($"maxwidth@{item.MaxWidth}");
					}
					if (item.MaxHeight.HasValue)
					{
						list2.Add($"maxheight@{item.MaxHeight}");
					}
					if (!item.Visible)
					{
						list2.Add("visible@false");
					}
					if (item.Centered)
					{
						list2.Add("center@true");
					}
					if (!item.LabelVisible)
					{
						list2.Add("labelVisible@false");
					}
					if (item.FontSize.HasValue)
					{
						list2.Add($"FontSize@{item.FontSize}");
					}
					if (item.LabelFontSize.HasValue)
					{
						list2.Add($"LabelFontSize@{item.LabelFontSize}");
					}
					if (item.FontFamily != null)
					{
						list2.Add("FontFamily@" + item.FontFamily);
					}
					if (item.Foreground != null)
					{
						list2.Add("Foreground@" + item.Foreground);
					}
					if (item.Data != null)
					{
						list2.Add("data@" + item.Data);
					}
					if (item.Unit != null)
					{
						list2.Add("unit@" + item.Unit);
					}
					list.Add(string.Join(",", list2));
				}
				else if (item.Type == "BorderLine")
				{
					List<string> list3 = new List<string> { "BorderLine:" };
					list3.Add($"x@{item.X}");
					list3.Add($"y@{item.Y}");
					list3.Add($"z@{item.Z}");
					if (item.MaxWidth.HasValue)
					{
						list3.Add($"maxwidth@{item.MaxWidth}");
					}
					if (item.MaxHeight.HasValue)
					{
						list3.Add($"maxheight@{item.MaxHeight}");
					}
					if (!item.Visible)
					{
						list3.Add("visible@false");
					}
					if (item.Centered)
					{
						list3.Add("center@true");
					}
					if (item.Fill != null)
					{
						list3.Add("Fill@" + item.Fill);
					}
					if (item.Data != null)
					{
						list3.Add("data@" + item.Data);
					}
					list.Add(string.Join(",", list3));
				}
			}
			File.WriteAllText(path, string.Join("\n", list));
		}
		catch (Exception ex)
		{
			Debug.WriteLine("[Soeyi] WriteSettingsLive failed: " + ex.Message);
		}
	}

	private void RefreshThemePreview()
	{
		if (_themePreviewImg == null)
		{
			return;
		}
		try
		{
			SoeyiTheme currentTheme = _vm.CurrentTheme;
			int num = 180;
			int num2 = 832;
			using SKBitmap sKBitmap = new SKBitmap(num, num2);
			using SKCanvas sKCanvas = new SKCanvas(sKBitmap);
			sKCanvas.Clear(SKColors.Black);
			ThemeRenderer.Render(sKCanvas, num, num2, _vm.HwSnapshot, currentTheme);
			using SKData sKData = sKBitmap.Encode(SKEncodedImageFormat.Png, 100);
			BitmapImage bitmapImage = new BitmapImage();
			using MemoryStream memoryStream = new MemoryStream(sKData.ToArray());
			memoryStream.Position = 0L;
			bitmapImage.SetSource(memoryStream.AsRandomAccessStream());
			_themePreviewImg.Source = bitmapImage;
		}
		catch (Exception ex)
		{
			Debug.WriteLine("RefreshThemePreview failed: " + ex.Message);
		}
	}

	private async Task ImportTheme()
	{
		await Task.Yield();
		try
		{
			string path = ShowOpenFileDialog(_vm.T("import"), "Theme Files (*.json;*.zip)|*.json;*.zip|JSON Theme (*.json)|*.json|ZIP Archive (*.zip)|*.zip");
			if (path != null)
			{
				SoeyiTheme theme = _vm.ThemeService.ImportFromFile(path);
				if (theme != null)
				{
					RebuildThemeDropdown();
					_currentThemeLabel.Text = theme.Name;
					_vm.SelectThemeCommand.Execute(theme.Name);
					_vm.StatusMessage = _vm.T("imported");
				}
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Exception ex3 = ex2;
			Exception ex4 = ex3;
			Exception ex5 = ex4;
			Exception ex6 = ex5;
			Exception ex7 = ex6;
			Exception ex8 = ex7;
			Exception ex9 = ex8;
			Exception ex10 = ex9;
			Exception ex11 = ex10;
			_vm.StatusMessage = "Import: " + ex11.Message;
		}
	}

	private void ExportTheme()
	{
		try
		{
			SoeyiTheme currentTheme = _vm.CurrentTheme;
			string name = currentTheme.Name;
			string filter = "ZIP 归档 (*.zip)|*.zip";
			string text = ShowSaveFileDialog(_vm.T("export"), filter, name + ".zip");
			if (text == null)
			{
				return;
			}
			using ZipArchive zipArchive = ZipFile.Open(text, ZipArchiveMode.Create);
			string value = JsonSerializer.Serialize(currentTheme, ThemeService.JsonOpts);
			ZipArchiveEntry zipArchiveEntry = zipArchive.CreateEntry(name + ".json");
			using (Stream stream = zipArchiveEntry.Open())
			{
				using StreamWriter streamWriter = new StreamWriter(stream);
				streamWriter.Write(value);
			}
			if (!string.IsNullOrEmpty(currentTheme.BackgroundImagePath) && File.Exists(currentTheme.BackgroundImagePath))
			{
				ZipArchiveEntry zipArchiveEntry2 = zipArchive.CreateEntry("background.png");
				using Stream destination = zipArchiveEntry2.Open();
				using FileStream fileStream = File.OpenRead(currentTheme.BackgroundImagePath);
				fileStream.CopyTo(destination);
			}
			string path = Path.Combine(currentTheme.FolderPath ?? "", "Setting.txt");
			if (File.Exists(path))
			{
				ZipArchiveEntry zipArchiveEntry3 = zipArchive.CreateEntry("Setting.txt");
				using Stream destination2 = zipArchiveEntry3.Open();
				using FileStream fileStream2 = File.OpenRead(path);
				fileStream2.CopyTo(destination2);
			}
			if (!string.IsNullOrEmpty(currentTheme.FolderPath))
			{
				string path2 = Path.Combine(currentTheme.FolderPath, "font");
				if (Directory.Exists(path2))
				{
					string[] files = Directory.GetFiles(path2);
					string[] array = files;
					string[] array2 = array;
					string[] array3 = array2;
					string[] array4 = array3;
					string[] array5 = array4;
					foreach (string text2 in array5)
					{
						string fileName = Path.GetFileName(text2);
						zipArchive.CreateEntryFromFile(text2, "font/" + fileName);
					}
				}
			}
			_vm.StatusMessage = _vm.T("exported") + ": " + Path.GetFileName(text);
		}
		catch (Exception ex)
		{
			_vm.StatusMessage = "Export: " + ex.Message;
		}
	}

	private void SaveAsTheme()
	{
		string text = "Theme_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
		string text2 = _vm.ThemeService.SaveAsNew(_vm.CurrentTheme, text);
		if (text2 != null)
		{
			RebuildThemeDropdown();
			_currentThemeLabel.Text = text;
			_vm.SelectThemeCommand.Execute(text);
			_vm.StatusMessage = _vm.T("saved");
		}
	}

	private void SaveCurrentTheme()
	{
		SoeyiTheme currentTheme = _vm.CurrentTheme;
		try
		{
			_vm.ThemeService.SaveTheme(currentTheme);
			_vm.StatusMessage = _vm.T("saved") + ": " + currentTheme.Name;
		}
		catch (Exception ex)
		{
			_vm.StatusMessage = "Save: " + ex.Message;
		}
	}

	private void SetDefaultTheme()
	{
		_vm.ThemeService.SetDefault(_vm.CurrentTheme.Name);
		_vm.StatusMessage = "⭐ Default: " + _vm.CurrentTheme.Name;
	}

	private async void DeleteCurrentTheme()
	{
		SoeyiTheme theme = _vm.CurrentTheme;
		if (theme.IsDefault)
		{
			_vm.StatusMessage = "Cannot delete default theme";
			return;
		}
		ContentDialog dlg = new ContentDialog
		{
			Title = "\ud83d\uddd1 " + _vm.T("delete"),
			Content = "Delete theme '" + theme.Name + "'?",
			PrimaryButtonText = _vm.T("delete"),
			SecondaryButtonText = _vm.T("editorCancel"),
			DefaultButton = ContentDialogButton.Secondary,
			XamlRoot = base.XamlRoot
		};
		if (await dlg.ShowAsync() != ContentDialogResult.Primary)
		{
			return;
		}
		try
		{
			if (!string.IsNullOrEmpty(theme.FolderPath) && Directory.Exists(theme.FolderPath))
			{
				Directory.Delete(theme.FolderPath, recursive: true);
			}
			_vm.ThemeService.RemoveTheme(theme.Name);
			RebuildThemeDropdown();
			_currentThemeLabel.Text = _vm.CurrentTheme.Name;
			_vm.StatusMessage = "Deleted: " + theme.Name;
		}
		catch (Exception ex)
		{
			_vm.StatusMessage = "Delete: " + ex.Message;
		}
	}

	private async Task NewTheme()
	{
		StackPanel panel = new StackPanel
		{
			Spacing = 12.0,
			Padding = new Thickness(4.0)
		};
		TextBox nameBox = new TextBox
		{
			PlaceholderText = "MyTheme",
			Header = _vm.T("editorTitle")
		};
		panel.Children.Add(nameBox);
		StackPanel dimRow = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		NumberBox wBox = new NumberBox
		{
			Value = 320.0,
			Minimum = 100.0,
			Maximum = 4000.0,
			Header = "W",
			Width = 120.0
		};
		NumberBox hBox = new NumberBox
		{
			Value = 1480.0,
			Minimum = 100.0,
			Maximum = 4000.0,
			Header = "H",
			Width = 120.0
		};
		dimRow.Children.Add(wBox);
		dimRow.Children.Add(hBox);
		panel.Children.Add(dimRow);
		ContentDialog dlg = new ContentDialog
		{
			Title = _vm.T("newTheme"),
			Content = panel,
			PrimaryButtonText = _vm.T("editorSave"),
			SecondaryButtonText = _vm.T("editorCancel"),
			DefaultButton = ContentDialogButton.Primary,
			XamlRoot = base.XamlRoot,
			RequestedTheme = ((!_vm.IsDarkTheme) ? ElementTheme.Light : ElementTheme.Dark)
		};
		if (await dlg.ShowAsync() == ContentDialogResult.Primary)
		{
			string name = (string.IsNullOrWhiteSpace(nameBox.Text) ? "MyTheme" : nameBox.Text.Trim());
			SoeyiTheme theme = new SoeyiTheme
			{
				Name = name,
				Width = (int)wBox.Value,
				Height = (int)hBox.Value,
				Type = 2,
				IsDefault = false,
				FillMode = 0
			};
			_vm.ThemeService.AddNewTheme(theme);
			RebuildThemeDropdown();
			_currentThemeLabel.Text = name;
			_vm.SelectThemeCommand.Execute(name);
			_vm.StatusMessage = _vm.T("imported");
		}
	}

	private async Task CropAndCreateTheme()
	{
		string srcPath = ShowOpenFileDialog(_vm.T("bgImage"), "Image Files|*.png;*.jpg;*.jpeg;*.bmp");
		if (srcPath == null)
		{
			return;
		}
		SKBitmap srcBmp = SKBitmap.Decode(srcPath);
		if (srcBmp == null)
		{
			_vm.StatusMessage = "Failed to load image";
			return;
		}
		int tw = 320;
		int th = 1480;
		float themeAspect = (float)tw / (float)th;
		float srcAspect = (float)srcBmp.Width / (float)srcBmp.Height;
		float ch;
		float cw;
		float cx;
		float cy;
		if (srcAspect > themeAspect)
		{
			ch = srcBmp.Height;
			cw = ch * themeAspect;
			cx = ((float)srcBmp.Width - cw) / 2f;
			cy = 0f;
		}
		else
		{
			cw = srcBmp.Width;
			ch = cw / themeAspect;
			cx = 0f;
			cy = ((float)srcBmp.Height - ch) / 2f;
		}
		int pw = 280;
		int ph = Math.Max(1, (int)((float)pw / themeAspect));
		if (ph > 460)
		{
			ph = 460;
			pw = Math.Max(1, (int)((float)ph * themeAspect));
		}
		StackPanel panel = new StackPanel
		{
			Spacing = 8.0,
			Width = 460.0
		};
		TextBox nameBox = new TextBox
		{
			PlaceholderText = "MyTheme",
			Header = _vm.T("editorTitle")
		};
		panel.Children.Add(nameBox);
		StackPanel dimRow = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		NumberBox wBox = new NumberBox
		{
			Value = tw,
			Minimum = 100.0,
			Maximum = 4000.0,
			Header = "W",
			Width = 120.0
		};
		NumberBox hBox = new NumberBox
		{
			Value = th,
			Minimum = 100.0,
			Maximum = 4000.0,
			Header = "H",
			Width = 120.0
		};
		dimRow.Children.Add(wBox);
		dimRow.Children.Add(hBox);
		panel.Children.Add(dimRow);
		Image cropImg = new Image
		{
			Width = pw,
			Height = ph,
			Stretch = Stretch.UniformToFill
		};
		panel.Children.Add(new Border
		{
			Child = cropImg,
			Width = pw + 2,
			Height = ph + 2,
			BorderBrush = new SolidColorBrush(Colors.Orange),
			BorderThickness = new Thickness(2.0),
			Background = new SolidColorBrush(Colors.DimGray),
			HorizontalAlignment = HorizontalAlignment.Center
		});
		TextBlock infoLbl = new TextBlock
		{
			FontSize = 12.0,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		panel.Children.Add(infoLbl);
		Slider zoomSlider = new Slider
		{
			Minimum = 0.5,
			Maximum = 2.0,
			Value = 1.0,
			StepFrequency = 0.05,
			Width = 320.0
		};
		panel.Children.Add(new TextBlock
		{
			Text = "\ud83d\udd0d Zoom",
			FontSize = 12.0
		});
		panel.Children.Add(zoomSlider);
		Slider xSlider = new Slider
		{
			Minimum = -1.0,
			Maximum = 1.0,
			Value = 0.0,
			StepFrequency = 0.01,
			Width = 320.0
		};
		panel.Children.Add(new TextBlock
		{
			Text = "↔ " + _vm.T("editorX"),
			FontSize = 12.0
		});
		panel.Children.Add(xSlider);
		Slider ySlider = new Slider
		{
			Minimum = -1.0,
			Maximum = 1.0,
			Value = 0.0,
			StepFrequency = 0.01,
			Width = 320.0
		};
		panel.Children.Add(new TextBlock
		{
			Text = "↕ " + _vm.T("editorY"),
			FontSize = 12.0
		});
		panel.Children.Add(ySlider);
		wBox.ValueChanged += OnDimChanged;
		hBox.ValueChanged += OnDimChanged;
		Refresh();
		bool dirty = true;
		zoomSlider.ValueChanged += MarkDirty;
		xSlider.ValueChanged += MarkDirty;
		ySlider.ValueChanged += MarkDirty;
		CancellationTokenSource cts = new CancellationTokenSource();
		Task.Run(async delegate
		{
			while (!cts.IsCancellationRequested)
			{
				await Task.Delay(80, cts.Token);
				if (!cts.IsCancellationRequested && dirty)
				{
					dirty = false;
					base.DispatcherQueue.TryEnqueue(delegate
					{
						Refresh();
					});
				}
			}
		}, cts.Token);
		ContentDialog dlg = new ContentDialog
		{
			Title = "✂ " + _vm.T("bgImage") + " — " + Path.GetFileName(srcPath),
			Content = new ScrollViewer
			{
				Content = panel,
				MaxHeight = 580.0
			},
			PrimaryButtonText = _vm.T("editorSave"),
			SecondaryButtonText = _vm.T("editorCancel"),
			DefaultButton = ContentDialogButton.Primary,
			XamlRoot = base.XamlRoot,
			RequestedTheme = ((!_vm.IsDarkTheme) ? ElementTheme.Light : ElementTheme.Dark)
		};
		ContentDialogResult result = await dlg.ShowAsync();
		cts.Cancel();
		if (result != ContentDialogResult.Primary)
		{
			srcBmp.Dispose();
			return;
		}
		string name = (string.IsNullOrWhiteSpace(nameBox.Text) ? Path.GetFileNameWithoutExtension(srcPath) : nameBox.Text.Trim());
		double zFinal = zoomSlider.Value;
		double pxF = xSlider.Value;
		double pyF = ySlider.Value;
		float zf = Math.Max((float)zFinal, cw / (float)srcBmp.Width);
		float zf2 = Math.Max((float)zFinal, ch / (float)srcBmp.Height);
		float zcw = cw / zf;
		float zch = ch / zf2;
		float ox = Math.Clamp(cx + (float)pxF * zcw * 0.5f, 0f, (float)srcBmp.Width - zcw);
		float oy = Math.Clamp(cy + (float)pyF * zch * 0.5f, 0f, (float)srcBmp.Height - zch);
		string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SoeyiWinUI", "Themes", name);
		Directory.CreateDirectory(folderPath);
		string destPath = Path.Combine(folderPath, "back.png");
		using (SKSurface surf = SKSurface.Create(new SKImageInfo(tw, th)))
		{
			surf.Canvas.DrawBitmap(srcBmp, SKRect.Create(ox, oy, zcw, zch), SKRect.Create(0f, 0f, tw, th));
			surf.Flush();
			using SKImage oi = surf.Snapshot();
			using SKData od = oi.Encode(SKEncodedImageFormat.Png, 90);
			using FileStream fs = File.OpenWrite(destPath);
			od.SaveTo(fs);
			srcBmp.Dispose();
			SoeyiTheme theme = new SoeyiTheme
			{
				Name = name,
				Width = tw,
				Height = th,
				Type = 2,
				IsDefault = false,
				FillMode = 0,
				BackgroundImagePath = destPath,
				BackgroundImage = "back.png",
				FolderPath = folderPath
			};
			_vm.ThemeService.AddNewTheme(theme);
			RebuildThemeDropdown();
			_currentThemeLabel.Text = name;
			_vm.SelectThemeCommand.Execute(name);
			_vm.StatusMessage = _vm.T("imported");
		}
		void MarkDirty(object _, object __)
		{
			dirty = true;
		}
		void OnDimChanged(object _, object __)
		{
			tw = (int)wBox.Value;
			th = (int)hBox.Value;
			themeAspect = (float)tw / (float)th;
			if (srcAspect > themeAspect)
			{
				ch = srcBmp.Height;
				cw = ch * themeAspect;
				cx = ((float)srcBmp.Width - cw) / 2f;
				cy = 0f;
			}
			else
			{
				cw = srcBmp.Width;
				ch = cw / themeAspect;
				cx = 0f;
				cy = ((float)srcBmp.Height - ch) / 2f;
			}
			ph = Math.Max(1, (int)((float)pw / themeAspect));
			if (ph > 460)
			{
				ph = 460;
				pw = Math.Max(1, (int)((float)ph * themeAspect));
			}
			RenderCropToImage(cropImg, srcBmp, cw, ch, cx, cy, pw, ph, zoomSlider.Value, xSlider.Value, ySlider.Value);
		}
		void Refresh()
		{
			double value = zoomSlider.Value;
			double value2 = xSlider.Value;
			double value3 = ySlider.Value;
			infoLbl.Text = $"\ud83d\udd0d {value * 100.0:F0}%  X:{value2:F2} Y:{value3:F2}";
			RenderCropToImage(cropImg, srcBmp, cw, ch, cx, cy, pw, ph, value, value2, value3);
		}
	}

	private static void RenderCropToImage(Image img, SKBitmap src, float cw, float ch, float cx, float cy, int pw, int ph, double z, double px, double py)
	{
		float num = Math.Max((float)z, cw / (float)src.Width);
		float num2 = Math.Max((float)z, ch / (float)src.Height);
		float num3 = cw / num;
		float num4 = ch / num2;
		float x = Math.Clamp(cx + (float)px * num3 * 0.5f, 0f, (float)src.Width - num3);
		float y = Math.Clamp(cy + (float)py * num4 * 0.5f, 0f, (float)src.Height - num4);
		using SKSurface sKSurface = SKSurface.Create(new SKImageInfo(pw, ph));
		sKSurface.Canvas.Clear(SKColors.DimGray);
		sKSurface.Canvas.DrawBitmap(src, SKRect.Create(x, y, num3, num4), SKRect.Create(0f, 0f, pw, ph));
		sKSurface.Flush();
		using SKImage sKImage = sKSurface.Snapshot();
		using SKData sKData = sKImage.Encode(SKEncodedImageFormat.Png, 80);
		MemoryStream memoryStream = new MemoryStream(sKData.ToArray());
		memoryStream.Position = 0L;
		BitmapImage bitmapImage = new BitmapImage();
		bitmapImage.SetSource(memoryStream.AsRandomAccessStream());
		img.Source = bitmapImage;
	}

	private void RebuildThemeDropdown()
	{
		if (_themeFlyout == null || _themeDropDown == null)
		{
			return;
		}
		_themeFlyout.Items.Clear();
		foreach (SoeyiTheme themePreset in _vm.ThemePresets)
		{
			MenuFlyoutItem menuFlyoutItem = new MenuFlyoutItem
			{
				Text = themePreset.Name
			};
			if (themePreset.FolderPath != null && themePreset.FolderPath.Contains("Programme"))
			{
				menuFlyoutItem.Icon = new FontIcon
				{
					Glyph = "\ue8b9"
				};
			}
			SoeyiTheme t = themePreset;
			menuFlyoutItem.Click += delegate
			{
				_vm.SelectThemeCommand.Execute(t.Name);
				_currentThemeLabel.Text = t.Name;
			};
			_themeFlyout.Items.Add(menuFlyoutItem);
		}
	}
}
