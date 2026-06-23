using SkiaSharp;
using SoeyiWinUI_v2.Models;
using SoeyiWinUI_v2.Services;

namespace SoeyiWinUI_v2.Rendering;

public static class ThemeRenderer
{
    // ─── Localization ───────────────────────
    public static string Lang = "zh-CN";

    private static string L(string zh, string en, string ja, string ko) => Lang switch
    {
        "zh-CN" => zh, "en-US" => en, "ja-JP" => ja, "ko-KR" => ko, _ => zh
    };

    // ─── Font ─────────────────────────────────────

    static readonly Dictionary<string, SKTypeface?> _fontCache = new();
    static SKTypeface? _cjkFallback;

    static SKTypeface GetCjk()
    {
        if (_cjkFallback != null) return _cjkFallback;
        string[] paths = { @"C:\Windows\Fonts\msyh.ttc", @"C:\Windows\Fonts\msyhbd.ttc",
                           @"C:\Windows\Fonts\simsun.ttc", @"C:\Windows\Fonts\malgun.ttf" };
        foreach (var p in paths)
            if (File.Exists(p)) { _cjkFallback = SKTypeface.FromFile(p); return _cjkFallback; }
        return SKTypeface.Default;
    }

    static SKTypeface? GetFont(string? fontFamily, string? themeDir = null)
    {
        if (string.IsNullOrEmpty(fontFamily)) return null;
        // Don't cache null — fonts from different theme dirs may succeed later
        if (_fontCache.TryGetValue(fontFamily, out var c) && c != null) return c;

        if (fontFamily.StartsWith("#"))
        {
            var name = fontFamily[1..].Trim();
            string[] weights = ["Heavy", "Medium", "Bold", "Light", "Regular", "Black"];
            var weight = weights.FirstOrDefault(w => fontFamily.Contains(w, StringComparison.OrdinalIgnoreCase));
            string? found = null;
            try
            {
                if (themeDir != null)
                {
                    var fd = Path.Combine(themeDir, "font");
                    if (Directory.Exists(fd))
                    {
                        var local = Directory.GetFiles(fd, "*.otf").Concat(Directory.GetFiles(fd, "*.ttf"));
                        if (weight != null)
                            found = local.FirstOrDefault(f => f.Contains(weight, StringComparison.OrdinalIgnoreCase));
                        found ??= local.FirstOrDefault();
                    }
                }
                if (found == null && fontFamily.Contains("思源", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var soeyiFonts = Directory.GetFiles(@"D:\Dir\SOEYI", "SourceHan*.otf", SearchOption.AllDirectories);
                        found = weight != null
                            ? soeyiFonts.FirstOrDefault(f => f.Contains(weight, StringComparison.OrdinalIgnoreCase))
                            : soeyiFonts.FirstOrDefault();
                    }
                    catch { }
                }
                if (found != null) { _fontCache[fontFamily] = SKTypeface.FromFile(found); return _fontCache[fontFamily]; }
            }
            catch { }
        }
        _fontCache[fontFamily] = null;
        return null;
    }

    static string TimeLabel() => Lang switch
    {
        "en-US" => DateTime.Now.ToString("h:mm:ss tt"),
        "zh-CN" => DateTime.Now.ToString("HH:mm:ss"),
        "ja-JP" => DateTime.Now.ToString("HH:mm:ss"),
        "ko-KR" => DateTime.Now.ToString("HH:mm:ss"),
        _ => DateTime.Now.ToString("HH:mm:ss")
    };

    // ─── Data Source ──────────────────────────────

    static string NormalizeDataSource(string? ds)
    {
        if (string.IsNullOrEmpty(ds)) return "";
        var d = ds.ToLowerInvariant().Replace(" ", "").Replace("_", "");
        if (d.Contains("currenttime") || d.Contains("time")) return "CurrentTimeShut";
        if (d.Contains("currentweek") || d.Contains("week")) return "CurrentWeek";
        if (d.Contains("currentdate") || d.Contains("dates") || d.Contains("date")) return "CurrentDate";
        if (d.Contains("cputemp") || (d.Contains("cpu") && d.Contains("temp")) || d == "cput" || d == "cputemp" || d == "cputemppu") return "CPUT";
        if (d.Contains("cpuusage") || (d.Contains("cpu") && d.Contains("usage")) || d == "cpuusage" || d == "cpuusagepu") return "CpuUsage";
        if (d.Contains("cputec") || d.Contains("cpupower") || d.Contains("cpupwr") || d.Contains("cpuwaste")) return "CpuPower";
        if (d.Contains("gputec") || d.Contains("gpupower") || d.Contains("gpupwr") || d.Contains("gpuwaste")) return "GpuPower";
        if (d.Contains("nightweather") || d.Contains("night") && d.Contains("weather")) return "NightWeather";
        if (d.Contains("heightweather") || d.Contains("dayweather") || d.Contains("highweather")) return "HeightWeather";
        if (d.Contains("lowweather")) return "LowWeather";
        if (d.Contains("weatherinfo") || d.Contains("weather")) return "WeatherInfo";
        if (d.Contains("gpuload") || d.Contains("gpuusage") || d.Contains("gpuusagepu")) return "GpuUsage";
        if (d.Contains("gputemp") || (d.Contains("gpu") && d.Contains("temp")) || d == "gput") return "GPUT";
        if (d.Contains("memoryfrequency") || d.Contains("memfreq") || (d.Contains("mem") && d.Contains("freq"))) return "MemoryFrequency";
        if (d.Contains("memoryusage") || d.Contains("memusage") || (d.Contains("mem") && d.Contains("usage"))) return "MemoryUsage";
        return ds;
    }

    static string WeekDayLabel(DayOfWeek d) => Lang switch
    {
        "zh-CN" => d switch { DayOfWeek.Monday => "周一", DayOfWeek.Tuesday => "周二", DayOfWeek.Wednesday => "周三", DayOfWeek.Thursday => "周四", DayOfWeek.Friday => "周五", DayOfWeek.Saturday => "周六", _ => "周日" },
        "ja-JP" => d switch { DayOfWeek.Monday => "月", DayOfWeek.Tuesday => "火", DayOfWeek.Wednesday => "水", DayOfWeek.Thursday => "木", DayOfWeek.Friday => "金", DayOfWeek.Saturday => "土", _ => "日" },
        "ko-KR" => d switch { DayOfWeek.Monday => "월", DayOfWeek.Tuesday => "화", DayOfWeek.Wednesday => "수", DayOfWeek.Thursday => "목", DayOfWeek.Friday => "금", DayOfWeek.Saturday => "토", _ => "일" },
        _ => d switch { DayOfWeek.Monday => "Mon", DayOfWeek.Tuesday => "Tue", DayOfWeek.Wednesday => "Wed", DayOfWeek.Thursday => "Thu", DayOfWeek.Friday => "Fri", DayOfWeek.Saturday => "Sat", _ => "Sun" }
    };

    static string ResolveData(string? ds, string? unit, HwSnapshot hw)
    {
        if (string.IsNullOrEmpty(ds)) return "";
        var n = NormalizeDataSource(ds);
        return n switch
        {
            "CpuUsage" => $"{hw.CpuUsage:F0}{unit ?? "%"}",
            "CPUT" => $"{hw.CpuTemp:F0}{unit ?? "°C"}",
            "GpuUsage" => $"{hw.GpuUsage:F0}{unit ?? "%"}",
            "GPUT" => $"{hw.GpuTemp:F0}{unit ?? "°C"}",
            "MemoryUsage" => $"{hw.RamUsageMb / 1024f:F1}{unit ?? "G"}",
            "MemoryFrequency" => $"{hw.MemoryFrequency:F0}{unit ?? "MHz"}",
            "CpuPower" => $"{hw.CpuPower:F1}{unit ?? "W"}",
            "GpuPower" => $"{hw.GpuPower:F1}{unit ?? "W"}",
            "NightWeather" => hw.HasWeather ? $"{hw.WeatherDesc} {hw.WeatherTemp:F0}°C" : "—",
            "HeightWeather" => hw.HasWeather ? $"{hw.WeatherHigh:F0}°C" : "—",
            "LowWeather" => hw.HasWeather ? $"{hw.WeatherLow:F0}°C" : "—",
            "WeatherInfo" => hw.HasWeather ? $"{hw.WeatherDesc} {hw.WeatherTemp:F0}°C" : "—",
            "CurrentTimeShut" => DateTime.Now.ToString("HH:mm"),
            "CurrentWeek" => WeekDayLabel(DateTime.Now.DayOfWeek),
            "CurrentDate" => Lang switch { "zh-CN" => DateTime.Now.ToString("MM月dd日"), "ja-JP" => DateTime.Now.ToString("MM月dd日"), "ko-KR" => DateTime.Now.ToString("MM월 dd일"), _ => DateTime.Now.ToString("dd/MM") },
            _ => ds
        };
    }

    static string? GetLabel(string? ds)
    {
        var n = NormalizeDataSource(ds);
        return n switch
        {
            "CpuUsage" => L("CPU占用", "CPU Load", "CPU負荷", "CPU 부하"),
            "CPUT" => L("CPU温度", "CPU Temp", "CPU温度", "CPU 온도"),
            "GpuUsage" => L("GPU占用", "GPU Load", "GPU負荷", "GPU 부하"),
            "GPUT" => L("GPU温度", "GPU Temp", "GPU温度", "GPU 온도"),
            "MemoryUsage" => L("内存", "Memory", "メモリ", "메모리"),
            "MemoryFrequency" => L("内存频率", "RAM Freq", "メモリ周波数", "메모리주파수"),
            "CpuPower" => L("CPU功耗", "CPU Power", "CPU消費電力", "CPU전력"),
            "GpuPower" => L("GPU功耗", "GPU Power", "GPU消費電力", "GPU전력"),
            "NightWeather" => L("天气", "Weather", "天気", "날씨"),
            "HeightWeather" => L("白天天气", "Day", "昼天気", "주간날씨"),
            "LowWeather" => L("低温", "Low", "低温", "저온"),
            "WeatherInfo" => L("天气", "Weather", "天気", "날씨"),
            "CurrentTimeShut" => L("时间", "Time", "時刻", "시간"),
            "CurrentWeek" => L("星期", "Weekday", "曜日", "요일"),
            "CurrentDate" => L("日期", "Date", "日付", "날짜"),
            _ => null
        };
    }

    public static string GetDataDisplayName(string? ds)
    {
        var n = NormalizeDataSource(ds);
        return n switch
        {
            "CpuUsage" => "cpuUsage", "CPUT" => "cpuTemp", "GpuUsage" => "gpuUsage",
            "GPUT" => "gpuTemp", "MemoryUsage" => "ram", "MemoryFrequency" => "memFreq",
            "CpuPower" => "cpuPower",
            "GpuPower" => "gpuPower",
            "NightWeather" => "nightWeather",
            "HeightWeather" => "heightWeather",
            "LowWeather" => "lowWeather",
            "WeatherInfo" => "weatherInfo",
            "CurrentTimeShut" => "curTime", "CurrentDate" => "curDate", "CurrentWeek" => "curWeek",
            _ => ds ?? ""
        };
    }

    // ─── Main Render ──────────────────────────────

    public static void Render(SKCanvas c, int w, int h, HwSnapshot hw, SoeyiTheme theme)
    {
        var settings = ThemeService.GetThemeSettings(theme);
        if (settings.Count > 0)
        {
            RenderProgramme(c, w, h, hw, theme, settings);
        }
        else
        {
            // Built-in dashboard
            var acc = SKColor.Parse("#0078D4");
            var bg = SKColor.Parse("#0a1a2a");
            switch (theme.Name.ToLowerInvariant())
            {
                case "dark": case "techn": RenderMinimal(c, w, h, hw); break;
                case "future": RenderNeon(c, w, h, hw, acc); break;
                case "vortex": RenderDashboard(c, w, h, hw, acc); break;
                case "reactor": RenderReactor(c, w, h, hw, acc); break;
                case "earth": case "compass": RenderEarth(c, w, h, hw, acc); break;
                case "white": RenderLight(c, w, h, hw); break;
                case "black": RenderBlack(c, w, h, hw); break;
                default: RenderDefault(c, w, h, hw, acc); break;
            }
        }
    }

    // ─── Programme Theme Renderer ──────────────────

    static void RenderProgramme(SKCanvas c, int w, int h, HwSnapshot hw,
        SoeyiTheme theme, List<SettingElement> settings)
    {
        c.Clear(SKColors.Black);
        int tw = (int)(theme.Width > 0 ? theme.Width : w);
        int th = (int)(theme.Height > 0 ? theme.Height : h);
        float sx = w / (float)tw, sy = h / (float)th;
        c.Scale(sx, sy);

        // Background image (fill-crop)
        SKBitmap? bgBmp = null;
        if (theme.BackgroundImagePath != null && File.Exists(theme.BackgroundImagePath))
        {
            try
            {
                bgBmp = SKBitmap.Decode(theme.BackgroundImagePath);
                if (bgBmp != null)
                {
                    float sc = Math.Max(tw / (float)bgBmp.Width, th / (float)bgBmp.Height);
                    float sw = bgBmp.Width * sc, sh = bgBmp.Height * sc;
                    float dx = (tw - sw) / 2f, dy = (th - sh) / 2f;
                    c.DrawBitmap(bgBmp, new SKRect(dx, dy, dx + sw, dy + sh));
                }
            }
            catch { bgBmp?.Dispose(); bgBmp = null; }
        }

        var fontDir = theme.FolderPath != null ? Path.Combine(theme.FolderPath, "font") : null;

        foreach (var elem in settings.OrderBy(e => e.Z))
        {
            if (!elem.Visible) continue;
            var text = ResolveData(elem.Data, elem.Unit, hw);
            if (text == null) continue;

            if (elem.Type == "BorderLine")
            {
                float val = 0;
                var n = NormalizeDataSource(elem.Data);
                if (n is "CpuUsage" or "GpuUsage" or "MemoryUsage")
                {
                    var parts = text.Replace("%", "").Replace("G", "").Trim().Split(' ');
                    float.TryParse(parts[0], out val);
                    if (n == "MemoryUsage") val = val / (float)(hw.TotalRamMb / 1024f) * 100;
                    val /= 100f;
                }
                float bw = (float)(elem.MaxWidth ?? 265), bh = (float)(elem.MaxHeight ?? 25);
                if (bw > 0 && bh > 0)
                {
                    var fill = TryParseColor(elem.Fill) ?? SKColors.Green;
                    using var bgP = new SKPaint { Color = new SKColor(40, 40, 60) };
                    c.DrawRoundRect(new SKRoundRect(new SKRect((float)elem.X, (float)elem.Y, (float)elem.X + bw, (float)elem.Y + bh), 2), bgP);
                    float fw = bw * Math.Clamp(val, 0, 1);
                    if (fw > 1)
                        using (var fgP = new SKPaint { Color = fill })
                            c.DrawRoundRect(new SKRoundRect(new SKRect((float)elem.X, (float)elem.Y, (float)elem.X + fw, (float)elem.Y + bh), 2), fgP);
                }
            }
            else if (elem.Type == "Text")
            {
                float fs = (float)(elem.FontSize ?? 25);
                var typeface = GetFont(elem.FontFamily, theme.FolderPath) ?? GetCjk();
                var fg = TryParseColor(elem.Foreground) ?? SKColors.White;

                // Adaptive foreground: if background is dark and text is dark, lighten
                if (bgBmp != null)
                {
                    int px = (int)((elem.X + fs * 2) * bgBmp.Width / (float)tw);
                    int py = (int)((elem.Y + fs * 0.8f) * bgBmp.Height / (float)th);
                    px = Math.Clamp(px, 0, bgBmp.Width - 1);
                    py = Math.Clamp(py, 0, bgBmp.Height - 1);
                    var bgPixel = bgBmp.GetPixel(px, py);
                    float bgLum = 0.2126f * bgPixel.Red + 0.7152f * bgPixel.Green + 0.0722f * bgPixel.Blue;
                    float fgLum = 0.2126f * fg.Red + 0.7152f * fg.Green + 0.0722f * fg.Blue;
                    // If both dark, auto-lighten to white with alpha
                    if (bgLum < 128 && fgLum < 100)
                    {
                        fg = SKColors.White;
                    }
                }

                // Compute draw positions (centered or manual)
                var label = GetLabel(elem.Data);
                float drawX = (float)elem.X;
                float labelX = (float)(elem.X + fs * 0.5f);
                if (elem.Centered)
                {
                    using var measureFont = new SKFont(typeface, fs);
                    using var measurePaint = new SKPaint { IsAntialias = true };
                    float textWidth = measurePaint.MeasureText(text ?? "");
                    drawX = (tw - textWidth) / 2f;
                    if (label != null)
                    {
                        float ls = (float)(elem.LabelFontSize ?? fs * 0.38f);
                        using var lMeasurePaint = new SKPaint { IsAntialias = true };
                        float labelWidth = lMeasurePaint.MeasureText(label);
                        labelX = (tw - labelWidth) / 2f;
                    }
                }

                // Draw label above value (fixed size for uniformity)
                if (label != null && elem.LabelVisible)
                {
                    float ls = (float)(elem.LabelFontSize ?? fs * 0.38f);
                    using var lf = new SKFont(typeface, ls);
                    using var lp = new SKPaint { Color = fg.WithAlpha(180), IsAntialias = true };
                    float ly = (float)elem.Y + fs * 0.3f;
                    c.DrawText(label, labelX, ly, lf, lp);
                }

                // Draw value
                using var font = new SKFont(typeface, fs);
                using var paint = new SKPaint { Color = fg, IsAntialias = true };
                c.DrawText(text, drawX, (float)elem.Y + fs * 1.35f, font, paint);
            }
            else if (!string.IsNullOrEmpty(elem.ImageFile))
            {
                var imgPath = theme.FolderPath != null ? Path.Combine(theme.FolderPath, elem.ImageFile) : elem.ImageFile;
                if (File.Exists(imgPath))
                {
                    try
                    {
                        using var img = SKBitmap.Decode(imgPath);
                        if (img != null)
                        {
                            float iw = (float)(elem.MaxWidth ?? img.Width);
                            float ih = (float)(elem.MaxHeight ?? img.Height);
                            c.DrawBitmap(img, new SKRect((float)elem.X, (float)elem.Y, (float)elem.X + iw, (float)elem.Y + ih));
                        }
                    }
                    catch { }
                }
            }
        }
        bgBmp?.Dispose();
    }

    static SKColor? TryParseColor(string? hex)
    {
        if (string.IsNullOrEmpty(hex)) return null;
        try { return SKColor.Parse(hex); } catch { return null; }
    }

    // ─── Built-in Dashboard Renderers ─────────────

    static void T(SKCanvas c, string s, float x, float y, SKFont f, SKPaint p, SKTextAlign a = SKTextAlign.Left)
    {
        p.TextAlign = a;
        c.DrawText(s, x, y, f, p);
    }

    static void Bar(SKCanvas c, float x, float y, float w, float h, float pct, SKColor fg, SKColor bg)
    {
        using var bp = new SKPaint { Color = bg };
        c.DrawRoundRect(new SKRoundRect(new SKRect(x, y, x + w, y + h), 3), bp);
        float fw = w * Math.Clamp(pct, 0, 1);
        if (fw > 1) using (var fp = new SKPaint { Color = fg }) c.DrawRoundRect(new SKRoundRect(new SKRect(x, y, x + fw, y + h), 3), fp);
    }

    static void RenderDefault(SKCanvas c, int w, int h, HwSnapshot hw, SKColor acc)
    {
        using var shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, h),
            new[] { SKColor.Parse("#1a1a2e"), SKColor.Parse("#16213e"), SKColor.Parse("#0f3460") },
            new float[] { 0, 0.5f, 1 }, SKShaderTileMode.Clamp);
        c.DrawRect(0, 0, w, h, new SKPaint { Shader = shader });

        // Header
        using var hdr = new SKPaint { Color = acc.WithAlpha(80) };
        c.DrawRoundRect(new SKRoundRect(new SKRect(8, 6, w - 8, 56), 12), hdr);
        using var tf = new SKFont(GetCjk(), 24);
        using var tp = new SKPaint { Color = SKColors.White, IsAntialias = true };
        T(c, L("系统监控", "System Monitor", "システム監視", "시스템 모니터"), w / 2f, 42, tf, tp, SKTextAlign.Center);
        using var lineP = new SKPaint { Color = acc, StrokeWidth = 2, IsAntialias = true };
        c.DrawLine(20, 64, w - 20, 64, lineP);

        // Split into 3 sections across full height
        float sectionH = (h - 70) / 3f;
        float y = 90;

        // CPU
        DrawSectionCard(c, w, ref y, 16, sectionH, L("处理器", "CPU", "プロセッサ", "프로세서"), hw.CpuTemp, hw.CpuUsage, acc);
        y += 20;
        // GPU
        DrawSectionCard(c, w, ref y, 16, y + sectionH > h ? h - y : sectionH, L("显卡", "GPU", "グラフィック", "그래픽"), hw.GpuTemp, hw.GpuUsage, acc);
        y += 20;
        // RAM
        float ramSectionH = h - y - 60;
        DrawRamCard(c, w, y, ramSectionH, hw, acc);

        // Footer
        using var fFont = new SKFont(GetCjk(), 12);
        using var fPaint = new SKPaint { Color = new SKColor(140, 140, 160), IsAntialias = true };
        T(c, TimeLabel(), w / 2f, h - 20, fFont, fPaint, SKTextAlign.Center);
    }

    static void DrawSectionCard(SKCanvas c, int w, ref float y, float x, float h, string label, float temp, float load, SKColor acc)
    {
        var cardBg = new SKColor(15, 25, 50);
        using var bgP = new SKPaint { Color = cardBg };
        c.DrawRoundRect(new SKRoundRect(new SKRect(10, y, w - 10, y + h), 14), bgP);

        // Label
        float innerY = y + 24;
        using var lf = new SKFont(GetCjk(), 18);
        using var lp = new SKPaint { Color = acc, IsAntialias = true };
        T(c, label, 26, innerY, lf, lp); innerY += 20;

        // Big temp value
        using var vf = new SKFont(GetCjk(), 62);
        using var vp = new SKPaint { Color = SKColors.White, IsAntialias = true };
        T(c, $"{temp:F0}°", 26, innerY + 54, vf, vp);

        // Load bar
        float barY = innerY + 80;
        float barW = w - 160;
        Bar(c, 26, barY, barW, 14, load / 100f, acc, acc.WithAlpha(30));
        using var sf = new SKFont(GetCjk(), 22);
        using var sp = new SKPaint { Color = new SKColor(200, 200, 220), IsAntialias = true };
        T(c, $"{load:F0}%", w - 26, barY + 4, sf, sp, SKTextAlign.Right);

        y += h + 4;
    }

    static void DrawRamCard(SKCanvas c, int w, float y, float h, HwSnapshot hw, SKColor acc)
    {
        var cardBg = new SKColor(15, 25, 50);
        using var bgP = new SKPaint { Color = cardBg };
        c.DrawRoundRect(new SKRoundRect(new SKRect(10, y, w - 10, y + h), 14), bgP);

        float innerY = y + 24;
        using var lf = new SKFont(GetCjk(), 18);
        using var lp = new SKPaint { Color = acc, IsAntialias = true };
        T(c, L("内存", "RAM", "メモリ", "메모리"), 26, innerY, lf, lp); innerY += 18;

        using var vf = new SKFont(GetCjk(), 52);
        using var vp = new SKPaint { Color = SKColors.White, IsAntialias = true };
        T(c, $"{hw.RamUsageMb / 1024f:F1}", 26, innerY + 44, vf, vp);
        using var sf = new SKFont(GetCjk(), 20);
        using var sp = new SKPaint { Color = new SKColor(180, 180, 200), IsAntialias = true };
        T(c, $"/{hw.TotalRamMb / 1024f:F0} GB", 180, innerY + 40, sf, sp);

        float barY = innerY + 70;
        float barW = w - 60;
        float ramPct = hw.RamUsageMb / (float)(hw.TotalRamMb > 0 ? hw.TotalRamMb : 1);
        Bar(c, 26, barY, barW, 14, ramPct, acc, acc.WithAlpha(30));
        T(c, $"{ramPct * 100:F0}%", w - 26, barY + 4, new SKFont(GetCjk(), 22), sp, SKTextAlign.Right);

        if (hw.NetDownKbps > 0)
        {
            var netY = barY + 40;
            using var nf = new SKFont(GetCjk(), 18);
            T(c, L("网络", "Network", "ネットワーク", "네트워크"), 26, netY, lf, lp);
            using var nv = new SKFont(GetCjk(), 14);
            T(c, $"↓{hw.NetDownKbps / 1000f:F1}M ↑{hw.NetUpKbps / 1000f:F1}M", 26, netY + 24, nv, sp);
        }
    }

    static void RenderMinimal(SKCanvas c, int w, int h, HwSnapshot hw)
    {
        c.Clear(SKColor.Parse("#0d0d0d"));
        float gap = (h - 100) / 5f;
        float y = gap;
        var bright = new SKColor(204, 204, 204);
        var dim = new SKColor(100, 100, 120);
        using var f = new SKFont(GetCjk(), 16);
        using var p = new SKPaint { Color = bright, IsAntialias = true };
        using var sf = new SKFont(GetCjk(), 14);
        using var sp = new SKPaint { Color = dim, IsAntialias = true };

        T(c, L("处理器", "CPU", "CPU", "CPU"), 20, y, new SKFont(GetCjk(), 18), new SKPaint { Color = SKColors.White, IsAntialias = true }); y += 28;
        T(c, $"{hw.CpuUsage:F0}%", 20, y, new SKFont(GetCjk(), 40), p);
        T(c, $"{hw.CpuTemp:F0}°C", 160, y, new SKFont(GetCjk(), 36), sp); y = gap * 2;

        T(c, L("显卡", "GPU", "GPU", "GPU"), 20, y, new SKFont(GetCjk(), 18), new SKPaint { Color = SKColors.White, IsAntialias = true }); y += 28;
        T(c, $"{hw.GpuUsage:F0}%", 20, y, new SKFont(GetCjk(), 40), p);
        T(c, $"{hw.GpuTemp:F0}°C", 160, y, new SKFont(GetCjk(), 36), sp); y = gap * 3;

        T(c, L("内存", "RAM", "メモリ", "메모리"), 20, y, new SKFont(GetCjk(), 18), new SKPaint { Color = SKColors.White, IsAntialias = true }); y += 28;
        T(c, $"{hw.RamUsageMb / 1024f:F1}/{hw.TotalRamMb / 1024f:F0}G", 20, y, sf, sp); y += 24;
        Bar(c, 20, y, w - 40, 10, hw.RamUsageMb / (float)(hw.TotalRamMb > 0 ? hw.TotalRamMb : 1), SKColors.White, new SKColor(60, 60, 80));

        T(c, TimeLabel(), w / 2f, h - 30, new SKFont(GetCjk(), 14),
            new SKPaint { Color = dim, IsAntialias = true }, SKTextAlign.Center);
    }

    static void RenderNeon(SKCanvas c, int w, int h, HwSnapshot hw, SKColor acc)
    {
        c.Clear(SKColor.Parse("#0a0a0a"));
        float gap = (h - 80) / 4f;
        float y = 40;
        using var hf = new SKFont(GetCjk(), 20);
        using var hp = new SKPaint { Color = acc, IsAntialias = true };
        using var sf = new SKFont(GetCjk(), 14);
        T(c, L("● 系统状态", "● System Status", "● システム状態", "● 시스템 상태"), 14, y, hf, hp); y = gap;

        NeonBar(c, 14, y, w - 28, 20, hw.CpuUsage / 100f, acc);
        T(c, $"{L("处理器", "CPU", "CPU", "CPU")} {hw.CpuUsage:F0}%  {hw.CpuTemp:F0}°C", 14, y + 32, sf, hp); y = gap * 2;

        NeonBar(c, 14, y, w - 28, 20, hw.GpuUsage / 100f, acc);
        T(c, $"{L("显卡", "GPU", "GPU", "GPU")} {hw.GpuUsage:F0}%  {hw.GpuTemp:F0}°C", 14, y + 32, sf, hp); y = gap * 3;

        NeonBar(c, 14, y, w - 28, 20, Math.Min(1, hw.RamUsageMb / (float)(hw.TotalRamMb > 0 ? hw.TotalRamMb : 1)), acc);
        T(c, $"{L("内存", "RAM", "メモリ", "메모리")} {hw.RamUsageMb / 1024f:F1}/{hw.TotalRamMb / 1024f:F0}G", 14, y + 32, sf, hp);
    }

    static void NeonBar(SKCanvas c, float x, float y, float w, float h, float pct, SKColor color)
    {
        using var bp = new SKPaint { Color = new SKColor(21, 21, 21) };
        c.DrawRoundRect(new SKRoundRect(new SKRect(x, y, x + w, y + h), 2), bp);
        float fw = w * Math.Clamp(pct, 0, 1);
        if (fw > 1)
        {
            using var gl = new SKPaint { Color = color.WithAlpha(60), MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 3) };
            c.DrawRoundRect(new SKRoundRect(new SKRect(x, y, x + fw, y + h), 2), gl);
            using var fg = new SKPaint { Color = color };
            c.DrawRoundRect(new SKRoundRect(new SKRect(x, y, x + fw, y + h), 2), fg);
        }
    }

    static void RenderDashboard(SKCanvas c, int w, int h, HwSnapshot hw, SKColor acc)
    {
        using var shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(w, h),
            new[] { SKColor.Parse("#1a1a2e"), acc.WithAlpha(40) }, SKShaderTileMode.Clamp);
        c.DrawRect(0, 0, w, h, new SKPaint { Shader = shader });
        float gap = h / 3f;
        using var big = new SKFont(GetCjk(), 56);
        using var med = new SKFont(GetCjk(), 24);
        using var sm = new SKFont(GetCjk(), 14);
        using var ap = new SKPaint { Color = acc, IsAntialias = true };
        using var wp = new SKPaint { Color = SKColors.White, IsAntialias = true };
        using var dp = new SKPaint { Color = new SKColor(150, 150, 170), IsAntialias = true };

        float cy = gap / 2f;
        T(c, $"{hw.CpuTemp:F0}°", w / 2f - 70, cy + 10, big, ap, SKTextAlign.Center);
        T(c, $"{L("处理器", "CPU", "CPU", "CPU")} {hw.CpuUsage:F0}%", w / 2f - 70, cy + 60, sm, dp, SKTextAlign.Center);
        T(c, $"{hw.GpuTemp:F0}°", w / 2f + 70, cy + 10, big, ap, SKTextAlign.Center);
        T(c, $"{L("显卡", "GPU", "GPU", "GPU")} {hw.GpuUsage:F0}%", w / 2f + 70, cy + 60, sm, dp, SKTextAlign.Center);

        cy = gap * 1.5f;
        T(c, L("内存", "RAM", "メモリ", "메모리"), w / 2f, cy - 20, med, wp, SKTextAlign.Center);
        T(c, $"{hw.RamUsageMb / 1024f:F1}/{hw.TotalRamMb / 1024f:F0}G", w / 2f, cy + 16, sm, dp, SKTextAlign.Center);
        Bar(c, 40, cy + 30, w - 80, 14, hw.RamUsageMb / (float)(hw.TotalRamMb > 0 ? hw.TotalRamMb : 1), acc, acc.WithAlpha(30));

        if (hw.NetDownKbps > 0)
            T(c, $"{L("网络", "Network", "ネットワーク", "네트워크")} ↓{hw.NetDownKbps / 1000f:F1}M ↑{hw.NetUpKbps / 1000f:F1}M", w / 2f, gap * 2.2f, sm, dp, SKTextAlign.Center);
        T(c, TimeLabel(), w / 2f, h - 40, sm, dp, SKTextAlign.Center);
    }

    static void RenderReactor(SKCanvas c, int w, int h, HwSnapshot hw, SKColor acc)
    {
        c.Clear(SKColor.Parse("#1a1a1a"));
        float gap = (h - 100) / 3f;
        using var big = new SKFont(GetCjk(), 72);
        using var hdr = new SKFont(GetCjk(), 20);
        using var lb = new SKFont(GetCjk(), 16);
        using var hp = new SKPaint { Color = acc, IsAntialias = true };
        using var vp = new SKPaint { Color = SKColors.White, IsAntialias = true };

        T(c, L("运行状态", "Status", "ステータス", "상태"), w / 2f, 40, hdr, hp, SKTextAlign.Center);
        float cy = gap;
        T(c, $"{hw.CpuTemp:F0}°", w / 2f, cy, big, vp, SKTextAlign.Center);
        T(c, $"{L("处理器占用", "CPU Load", "CPU負荷", "CPU 부하")}: {hw.CpuUsage:F0}%", w / 2f, cy + 60, lb, hp, SKTextAlign.Center);
        Bar(c, 30, cy + 76, w - 60, 10, hw.CpuUsage / 100f, acc, acc.WithAlpha(30));

        cy = gap * 2;
        T(c, $"{hw.GpuTemp:F0}°", w / 2f, cy, big, vp, SKTextAlign.Center);
        T(c, $"{L("显卡占用", "GPU Load", "GPU負荷", "GPU 부하")}: {hw.GpuUsage:F0}%", w / 2f, cy + 60, lb, hp, SKTextAlign.Center);
        Bar(c, 30, cy + 76, w - 60, 10, hw.GpuUsage / 100f, acc, acc.WithAlpha(30));

        cy = h - 90;
        T(c, $"{L("内存", "RAM", "メモリ", "메모리")} {hw.RamUsageMb / 1024f:F1}/{hw.TotalRamMb / 1024f:F0}G", w / 2f, cy, lb, hp, SKTextAlign.Center);
        Bar(c, 30, cy + 16, w - 60, 10, hw.RamUsageMb / (float)(hw.TotalRamMb > 0 ? hw.TotalRamMb : 1), acc, acc.WithAlpha(30));
        T(c, TimeLabel(), w / 2f, h - 30, new SKFont(GetCjk(), 12),
            new SKPaint { Color = new SKColor(130, 130, 150), IsAntialias = true }, SKTextAlign.Center);
    }

    static void RenderEarth(SKCanvas c, int w, int h, HwSnapshot hw, SKColor acc)
    {
        c.Clear(SKColor.Parse("#e8f5e9"));
        var dp = new SKColor(40, 40, 60);
        float gap = (h - 60) / 4f;
        float y = 30;
        using var hf = new SKFont(GetCjk(), 18);
        using var hp = new SKPaint { Color = acc, IsAntialias = true };
        using var lf = new SKFont(GetCjk(), 16);
        using var lp = new SKPaint { Color = dp, IsAntialias = true };
        using var sf = new SKFont(GetCjk(), 14);

        T(c, L("系统健康", "System Health", "システム健全", "시스템 상태"), 16, y, hf, hp); y = gap;
        T(c, L("处理器", "CPU", "CPU", "CPU"), 16, y, lf, lp); y += 24;
        Bar(c, 16, y, w - 32, 12, hw.CpuUsage / 100f, acc, acc.WithAlpha(40));
        T(c, $"{hw.CpuUsage:F0}%  {hw.CpuTemp:F0}°C", w - 16, y + 10, sf, lp, SKTextAlign.Right);

        y = gap * 2;
        T(c, L("显卡", "GPU", "GPU", "GPU"), 16, y, lf, lp); y += 24;
        Bar(c, 16, y, w - 32, 12, hw.GpuUsage / 100f, acc, acc.WithAlpha(40));
        T(c, $"{hw.GpuUsage:F0}%  {hw.GpuTemp:F0}°C", w - 16, y + 10, sf, lp, SKTextAlign.Right);

        y = gap * 3;
        T(c, L("内存", "RAM", "メモリ", "메모리"), 16, y, lf, lp); y += 24;
        Bar(c, 16, y, w - 32, 12, hw.RamUsageMb / (float)(hw.TotalRamMb > 0 ? hw.TotalRamMb : 1), acc, acc.WithAlpha(40));
        T(c, $"{hw.RamUsageMb / 1024f:F1}/{hw.TotalRamMb / 1024f:F0}G", w - 16, y + 10, sf, lp, SKTextAlign.Right);

        T(c, TimeLabel(), w / 2f, h - 25, sf, new SKPaint { Color = new SKColor(120, 120, 140), IsAntialias = true }, SKTextAlign.Center);
    }

    static void RenderLight(SKCanvas c, int w, int h, HwSnapshot hw)
    {
        c.Clear(SKColor.Parse("#f3f3f3"));
        var dp = new SKColor(40, 40, 60);
        float y = 28;
        using var f = new SKFont(GetCjk(), 13);
        using var p = new SKPaint { Color = dp, IsAntialias = true };
        T(c, $"{L("处理器", "CPU", "CPU", "CPU")} {hw.CpuUsage:F0}%  {hw.CpuTemp:F0}°C", 16, y, f, p); y += 20;
        T(c, $"{L("显卡", "GPU", "GPU", "GPU")} {hw.GpuUsage:F0}%  {hw.GpuTemp:F0}°C", 16, y, f, p); y += 20;
        T(c, $"{L("内存", "RAM", "メモリ", "메모리")}  {hw.RamUsageMb / 1024f:F1}/{hw.TotalRamMb / 1024f:F0}G", 16, y, f, p);
    }

    static void RenderBlack(SKCanvas c, int w, int h, HwSnapshot hw)
    {
        c.Clear(SKColors.Black);
        float y = 28;
        using var f = new SKFont(GetCjk(), 13);
        using var p = new SKPaint { Color = new SKColor(204, 204, 204), IsAntialias = true };
        T(c, $"{L("处理器", "CPU", "CPU", "CPU")} {hw.CpuUsage:F0}%  {hw.CpuTemp:F0}°C", 16, y, f, p); y += 20;
        T(c, $"{L("显卡", "GPU", "GPU", "GPU")} {hw.GpuUsage:F0}%  {hw.GpuTemp:F0}°C", 16, y, f, p); y += 20;
        T(c, $"{L("内存", "RAM", "メモリ", "메모리")}  {hw.RamUsageMb / 1024f:F1}/{hw.TotalRamMb / 1024f:F0}G", 16, y, f, p);
    }
}
