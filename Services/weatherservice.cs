using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Timer = System.Timers.Timer;

namespace SoeyiWinUI_v2.Services;

public class WeatherSnapshot
{
    public float Temperature { get; set; }
    public float FeelsLike { get; set; }
    public float Humidity { get; set; }
    public float WindSpeed { get; set; }
    public int WeatherCode { get; set; }
    public string WeatherDescription { get; set; } = "";
    public float HighTemp { get; set; }
    public float LowTemp { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool HasData { get; set; }

    // ── WMO code → short Chinese weather text ──
    private static string WmoText(int code) => code switch
    {
        0 => "晴", 1 => "少云", 2 => "多云", 3 => "阴",
        45 or 48 => "雾",
        51 or 53 or 55 => "毛毛雨", 56 or 57 => "冻毛毛雨",
        61 or 63 or 65 => "雨", 66 or 67 => "冻雨",
        71 or 73 or 75 => "雪", 77 => "雪粒",
        80 or 81 or 82 => "阵雨", 85 or 86 => "阵雪",
        95 => "雷暴", 96 or 99 => "冰雹雷暴",
        _ => "未知"
    };

    /// <summary>Time period label based on current hour</summary>
    public static string TimePeriodLabel(DateTime? time = null)
    {
        var h = (time ?? DateTime.Now).Hour;
        if (h >= 0 && h <= 5) return "凌晨";
        if (h >= 6 && h <= 8) return "早晨";
        if (h >= 9 && h <= 11) return "上午";
        if (h >= 12 && h <= 13) return "中午";
        if (h >= 14 && h <= 17) return "下午";
        if (h >= 18 && h <= 19) return "傍晚";
        return "夜间"; // 20-23
    }

    /// <summary>Weather description with time-period prefix (e.g. "下午少云")</summary>
    public string PeriodDescription => TimePeriodLabel() + WmoText(WeatherCode);

    /// <summary>Night weather: current if already night, otherwise tonight's outlook</summary>
    public string NightDescription
    {
        get
        {
            var h = DateTime.Now.Hour;
            // If it's already nighttime (20-5), show current with night prefix
            if (h >= 20 || h <= 5)
                return "夜间" + WmoText(WeatherCode);
            // Daytime: show tonight's forecast (use current as proxy since Open-Meteo free has no overnight)
            return "今晚" + WmoText(WeatherCode);
        }
    }

    /// <summary>Current weather description in Chinese (short)</summary>
    public string CurrentDescription => WmoText(WeatherCode);

    /// <summary>Current weather description in Chinese (full)</summary>
    public string FullDescription => TimePeriodLabel() + "，" + WmoText(WeatherCode);
}

public class WeatherService : IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly HttpClient _http = new(new SocketsHttpHandler { UseProxy = true, AutomaticDecompression = System.Net.DecompressionMethods.All }) { Timeout = TimeSpan.FromSeconds(10) };
    private Timer? _timer;
    private readonly double _lat, _lon;
    private WeatherSnapshot _current = new();
    private readonly object _lock = new();

    public event Action<WeatherSnapshot>? Updated;
    public WeatherSnapshot Current { get { lock (_lock) return _current; } }

    public WeatherService(double lat = 39.9042, double lon = 116.4074)
    {
        _lat = lat; _lon = lon;
    }

    public async Task StartAsync(int intervalMs = 1_800_000) // default 30 min
    {
        await FetchAsync();
        _timer = new Timer(intervalMs) { AutoReset = true };
        _timer.Elapsed += async (_, _) => await FetchAsync();
        _timer.Start();
    }

    private async Task FetchAsync()
    {
        try
        {
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={_lat}&longitude={_lon}" +
                      "&current=temperature_2m,relative_humidity_2m,apparent_temperature,weather_code,wind_speed_10m" +
                      "&daily=temperature_2m_max,temperature_2m_min&timezone=Asia/Shanghai&forecast_days=1";

            var json = await _http.GetStringAsync(url);
            var doc = JsonDocument.Parse(json);

            var current = doc.RootElement.GetProperty("current");
            var daily = doc.RootElement.GetProperty("daily");

            var snap = new WeatherSnapshot
            {
                Temperature = current.GetProperty("temperature_2m").GetSingle(),
                FeelsLike = current.GetProperty("apparent_temperature").GetSingle(),
                Humidity = current.GetProperty("relative_humidity_2m").GetSingle(),
                WindSpeed = current.GetProperty("wind_speed_10m").GetSingle(),
                WeatherCode = current.GetProperty("weather_code").GetInt32(),
                HighTemp = daily.GetProperty("temperature_2m_max")[0].GetSingle(),
                LowTemp = daily.GetProperty("temperature_2m_min")[0].GetSingle(),
                UpdatedAt = DateTime.Now,
                HasData = true
            };
            snap.WeatherDescription = snap.PeriodDescription;

            lock (_lock) { _current = snap; }
            Updated?.Invoke(snap);

            System.Diagnostics.Debug.WriteLine($"Weather: {snap.Temperature}°C ({snap.WeatherDescription}) H:{snap.HighTemp} L:{snap.LowTemp}");
            Logger.Info($"Weather: {snap.Temperature:F1}°C ({snap.WeatherDescription}) H:{snap.HighTemp:F0} L:{snap.LowTemp:F0}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Weather fetch failed: {ex.Message}");
            Logger.Error($"Weather fetch failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _timer?.Stop(); _timer?.Dispose(); _http?.Dispose();
    }
}
