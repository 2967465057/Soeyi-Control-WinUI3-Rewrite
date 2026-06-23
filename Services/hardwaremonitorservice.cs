using System.Diagnostics;
using System.Management;
using NLog;
using Timer = System.Timers.Timer;

namespace SoeyiWinUI_v2.Services;

public class HwSnapshot
{
    public float CpuUsage, CpuTemp, GpuUsage, GpuTemp;
    public float RamUsageMb, NetUp, NetDown;
    public ulong TotalRamMb;
    public float NetUpKbps => NetUp;
    public float NetDownKbps => NetDown;
    public float MemoryFrequency;
    public float CpuPower;
    public float GpuPower;
    // Weather
    public float WeatherTemp, WeatherFeelsLike, WeatherHumidity, WeatherWind;
    public float WeatherHigh, WeatherLow;
    public int WeatherCode;
    public string WeatherDesc { get; set; } = "";
    public string WeatherNightDesc { get; set; } = "";
    public bool HasWeather { get; set; }
    public HwSnapshot() { }
    public HwSnapshot(float cpuUsage, float cpuTemp, float gpuUsage, float gpuTemp, float ramUsageMb, ulong totalRamMb, float netUp, float netDown)
    { CpuUsage=cpuUsage; CpuTemp=cpuTemp; GpuUsage=gpuUsage; GpuTemp=gpuTemp; RamUsageMb=ramUsageMb; TotalRamMb=totalRamMb; NetUp=netUp; NetDown=netDown; }
}

public class HardwareMonitorService : IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private FanControlSensorReader? _fcReader;
    private WeatherService? _weather;
    private Timer? _timer;
    private readonly object _lock = new();
    private bool _started;
    private int _sample;

    private float _cpuUsage, _cpuTemp, _gpuUsage, _gpuTemp;
    private float _ramUsageMb; private ulong _totalRamMb;
    private float _netDown, _netUp; private float _memoryFrequency; private float _cpuPower; private float _cpuBaseClock; private float _gpuPower;
    private int _gpuSampleCounter;
    private long _prevNetBytesDown, _prevNetBytesUp;
    private DateTime _prevNetSample = DateTime.UtcNow;

    public event Action<HwSnapshot>? Updated;

    /// <param name="useFanControl">If true, attempt FanControl IPC as primary sensor source</param>
    public HardwareMonitorService(bool useFanControl = true)
    {
        if (useFanControl)
        {
            _fcReader = new FanControlSensorReader();
        }
        _weather = new WeatherService();
    }

    public void Start(int intervalMs = 1000)
    {
        if (_started) return;
        lock (_lock) { if (_started) return; _started = true; }

        // Try FanControl IPC
        if (_fcReader?.Connect() == true)
            Logger.Info("Hardware monitor: FanControl IPC active");
        else
            Logger.Info("Hardware monitor: WMI + nvidia-smi fallback");

        // WMI total RAM
        try { using var w = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"); foreach (ManagementObject mo in w.Get()) if (mo["TotalPhysicalMemory"] is ulong v) _totalRamMb = v/(1024*1024); } catch { }
        if (_totalRamMb < 500) _totalRamMb = 32000;

        // Start weather service
        _ = _weather?.StartAsync();

        _timer = new Timer(intervalMs) { AutoReset = true };
        _timer.Elapsed += (_, _) => Sample();
        _timer.Start();
    }

    public void Stop() { _started = false; _timer?.Stop(); _timer?.Dispose(); _timer = null; }

    private void Sample()
    {
        lock (_lock)
        {
            // 1. FanControl IPC (primary for CPU/GPU temp+usage if available)
            if (_fcReader?.Available == true)
            {
                _fcReader.Poll();
                if (_fcReader.CpuTemperature > 0) _cpuTemp = _fcReader.CpuTemperature;
                if (_fcReader.CpuUsage > 0) _cpuUsage = _fcReader.CpuUsage;
                if (_fcReader.GpuTemperature > 0) _gpuTemp = _fcReader.GpuTemperature;
                if (_fcReader.GpuUsage > 0) _gpuUsage = _fcReader.GpuUsage;
            }

            // 2. WMI CPU usage fallback (only if FanControl didn't provide)
            if (_cpuUsage < 1)
            {
                try { using var w = new ManagementObjectSearcher("SELECT LoadPercentage FROM Win32_Processor"); foreach (ManagementObject mo in w.Get()) if (mo["LoadPercentage"] is ushort v) _cpuUsage = v; } catch { }
            }

            // 3. WMI RAM
            try { using var w = new ManagementObjectSearcher("SELECT FreePhysicalMemory,TotalVisibleMemorySize FROM Win32_OperatingSystem"); foreach (ManagementObject mo in w.Get()) if (mo["FreePhysicalMemory"] is ulong f && mo["TotalVisibleMemorySize"] is ulong t) { _totalRamMb = t/1024; _ramUsageMb = (t-f)/1024f; } } catch { }

            // 4. nvidia-smi GPU fallback
            if (_gpuTemp < 1 && _gpuSampleCounter++ % 5 == 0)
            {
                try { using var p = Process.Start(new ProcessStartInfo { FileName="nvidia-smi", Arguments="--query-gpu=temperature.gpu,utilization.gpu --format=csv,noheader,nounits", RedirectStandardOutput=true, UseShellExecute=false, CreateNoWindow=true });
                    if (p!=null) { var o=p.StandardOutput.ReadToEnd(); p.WaitForExit(3000); var x=o.Trim().Split(','); if (x.Length>=2) { if (float.TryParse(x[0].Trim(),out var t)) _gpuTemp=t; if (float.TryParse(x[1].Trim(),out var u)) _gpuUsage=u; } } } catch { }
            }

            try { SampleNetwork(); } catch { }
            if (_memoryFrequency < 100) { try { using var w = new ManagementObjectSearcher("SELECT Speed FROM Win32_PhysicalMemory WHERE Speed IS NOT NULL"); foreach (ManagementObject mo in w.Get()) if (mo["Speed"] is uint m && m>100) { _memoryFrequency=m; break; } } catch { } if (_memoryFrequency<100) _memoryFrequency=3200; }
            if (_cpuBaseClock < 100) { try { using var w = new ManagementObjectSearcher("SELECT MaxClockSpeed FROM Win32_Processor"); foreach (ManagementObject mo in w.Get()) if (mo["MaxClockSpeed"] is uint m && m>100) { _cpuBaseClock=m; break; } } catch { } if (_cpuBaseClock<100) _cpuBaseClock=4000; }
            _cpuPower = 15f + (_cpuUsage / 100f) * (_cpuBaseClock / 1000f * 12f);
            _gpuPower = 10f + (_gpuUsage / 100f) * 140f;

            if (_sample++ % 60 == 0)
                Logger.Info("HW: CPU={0}%/{1:F1}°C GPU={2}%/{3:F1}°C RAM={4:F0}/{5}M",
                    _cpuUsage, _cpuTemp, _gpuUsage, _gpuTemp, _ramUsageMb, _totalRamMb);
        }
        var snap = new HwSnapshot(_cpuUsage, _cpuTemp, _gpuUsage, _gpuTemp, _ramUsageMb, _totalRamMb, _netUp, _netDown);
        snap.MemoryFrequency = _memoryFrequency;
        snap.CpuPower = _cpuPower;
        snap.GpuPower = _gpuPower;
        if (_weather?.Current.HasData == true) { var w = _weather.Current; snap.WeatherTemp = w.Temperature; snap.WeatherFeelsLike = w.FeelsLike; snap.WeatherHumidity = w.Humidity; snap.WeatherWind = w.WindSpeed; snap.WeatherHigh = w.HighTemp; snap.WeatherLow = w.LowTemp; snap.WeatherCode = w.WeatherCode; snap.WeatherDesc = w.CurrentDescription; snap.WeatherNightDesc = w.NightDescription; snap.HasWeather = true; }
        Updated?.Invoke(snap);
    }

    private void SampleNetwork() { try {
        var ifs = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces(); if (ifs==null||ifs.Length==0) return;
        long td=0,tu=0; foreach (var n in ifs) try { if (n.OperationalStatus!=System.Net.NetworkInformation.OperationalStatus.Up) continue;
        var s=n.GetIPStatistics(); td+=s.BytesReceived; tu+=s.BytesSent; } catch { }
        var now=DateTime.UtcNow; var el=(now-_prevNetSample).TotalSeconds;
        if (el>0.5&&td>_prevNetBytesDown) { _netDown=(float)((td-_prevNetBytesDown)*8/el/1000); _netUp=(float)((tu-_prevNetBytesUp)*8/el/1000); }
        _prevNetBytesDown=td; _prevNetBytesUp=tu; _prevNetSample=now;
    } catch { } }

    public HwSnapshot GetSnapshot() { lock (_lock) { var s = new HwSnapshot(_cpuUsage, _cpuTemp, _gpuUsage, _gpuTemp, _ramUsageMb, _totalRamMb, _netUp, _netDown); s.MemoryFrequency = _memoryFrequency; s.CpuPower = _cpuPower; s.GpuPower = _gpuPower; if (_weather?.Current.HasData == true) { var w = _weather.Current; s.WeatherTemp = w.Temperature; s.WeatherFeelsLike = w.FeelsLike; s.WeatherHumidity = w.Humidity; s.WeatherWind = w.WindSpeed; s.WeatherHigh = w.HighTemp; s.WeatherLow = w.LowTemp; s.WeatherCode = w.WeatherCode; s.WeatherDesc = w.CurrentDescription; s.WeatherNightDesc = w.NightDescription; s.HasWeather = true; } return s; } }
    public bool FanControlActive => _fcReader?.Available == true;

    public void SetFanControl(bool enabled)
    {
        if (enabled && _fcReader?.Available != true) _fcReader?.Connect();
        else if (!enabled) { _fcReader?.Dispose(); _fcReader = null; }
    }
    public void Dispose() { Stop(); _fcReader?.Dispose(); _weather?.Dispose(); }
}
