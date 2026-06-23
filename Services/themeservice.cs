using System.Text.Json;
using NLog;
using SoeyiWinUI_v2.Models;

namespace SoeyiWinUI_v2.Services;

/// <summary>
/// Theme service compatible with original SOEYI theme format.
/// Discovers themes from original SOEYI directories, supports import/export/save.
/// </summary>
public class ThemeService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly ConfigService _config;
    private readonly string _dataDir;

    public List<SoeyiTheme> Themes { get; } = new();
    public SoeyiTheme CurrentTheme { get; private set; }
    public bool IsDarkTheme { get; set; }

    public event EventHandler<SoeyiTheme>? ThemeChanged;

    public static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public ThemeService(ConfigService config)
    {
        _config = config;
        _dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SoeyiWinUI", "Themes");
        Directory.CreateDirectory(_dataDir);

        DiscoverThemes();

        var lastId = _config.Get("SelectedThemeId", "");
        var lastName = _config.Get("SelectedThemeName", "Default");
        CurrentTheme = Themes.FirstOrDefault(t => t.Name == lastName || t.Name == lastId)
                       ?? Themes.FirstOrDefault()
                       ?? CreateDefault();
    }

    // ─── Theme Discovery ──────────────────────────

    private void DiscoverThemes()
    {
        // 1. Programme themes FIRST (richer - have images, Setting.txt, custom fonts)
        string[] progDirs = {
            @"D:\Dir\SOEYI\Programme",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SOEYI", "Programme")
        };

        foreach (var dir in progDirs)
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var sub in Directory.GetDirectories(dir))
            {
                var name = Path.GetFileName(sub);
                var jsonPath = Path.Combine(sub, $"{name}.json");
                if (!File.Exists(jsonPath)) continue;
                try
                {
                    var theme = JsonSerializer.Deserialize<SoeyiTheme>(
                        File.ReadAllText(jsonPath), JsonOpts);
                    if (theme == null) continue;
                    theme.FolderPath = sub;
                    var backPng = Path.Combine(sub, "back.png");
                    if (File.Exists(backPng)) theme.BackgroundImagePath = backPng;
                    if (!Themes.Any(t => t.Name == theme.Name))
                        Themes.Add(theme);
                }
                catch (Exception ex) { Logger.Debug(ex, "Skip programme: {0}", sub); }
            }
        }

        // 2. Built-in themes from original SOEYI ThemeScheme (fallback, skip if Programme has same name)
        string[] soeyiDirs = {
            @"D:\Dir\SOEYI\ThemeScheme",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SOEYI", "ThemeScheme")
        };

        foreach (var dir in soeyiDirs)
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var f in Directory.GetFiles(dir, "*.json"))
            {
                try
                {
                    var theme = JsonSerializer.Deserialize<SoeyiTheme>(
                        File.ReadAllText(f), JsonOpts);
                    if (theme == null || string.IsNullOrEmpty(theme.Name)) continue;
                    // Skip if Programme theme with same name already loaded
                    if (Themes.Any(t => t.Name == theme.Name)) continue;
                    theme.FolderPath = dir;
                    Themes.Add(theme);
                }
                catch (Exception ex) { Logger.Debug(ex, "Skip theme: {0}", f); }
            }
        }

        // 3. Our own saved themes
        foreach (var f in Directory.GetFiles(_dataDir, "*.json"))
        {
            try
            {
                var theme = JsonSerializer.Deserialize<SoeyiTheme>(
                    File.ReadAllText(f), JsonOpts);
                if (theme == null || string.IsNullOrEmpty(theme.Name)) continue;
                theme.FolderPath = _dataDir;
                // If a subfolder exists (from crop/create), use it for Setting.txt / bg image
                var subDir = Path.Combine(_dataDir, theme.Name);
                if (Directory.Exists(subDir)) theme.FolderPath = subDir;
                if (!Themes.Any(t => t.Name == theme.Name))
                    Themes.Add(theme);
            }
            catch { }
        }

        Logger.Info("Discovered {0} themes", Themes.Count);
    }

    private static SoeyiTheme CreateDefault()
    {
        return new SoeyiTheme
        {
            IsDefault = true, Type = 1, Name = "Default",
            Width = 320, Height = 1480, FillMode = 0,
            DisplayDirection = 0, FontDirection = 0
        };
    }

    // ─── Theme Operations ─────────────────────────

    public void SelectTheme(string themeName)
    {
        var theme = Themes.FirstOrDefault(t => t.Name == themeName);
        if (theme == null) return;
        CurrentTheme = theme;
        _config.Set("SelectedThemeName", theme.Name);
        _config.Save();
        ThemeChanged?.Invoke(this, theme);
        Logger.Info("Selected theme: {0}", theme.Name);
    }

    public SoeyiTheme? GetTheme(string name) =>
        Themes.FirstOrDefault(t => t.Name == name);

    // ─── Import / Export ──────────────────────────

    public SoeyiTheme? ImportFromZip(string zipPath)
    {
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "SoeyiImport_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, tempDir);

                // Find the theme JSON
                var jsonFile = Directory.GetFiles(tempDir, "*.json").FirstOrDefault();
                if (jsonFile == null) return null;

                var json = File.ReadAllText(jsonFile);
                var theme = JsonSerializer.Deserialize<SoeyiTheme>(json, JsonOpts);
                if (theme == null || string.IsNullOrEmpty(theme.Name)) return null;

                // Create theme subfolder
                var themeDir = Path.Combine(_dataDir, theme.Name);
                Directory.CreateDirectory(themeDir);
                theme.FolderPath = themeDir;

                // Find and copy background image
                var bgExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp" };
                foreach (var ext in bgExtensions)
                {
                    var candidate = Path.Combine(tempDir, "background" + ext);
                    if (!File.Exists(candidate)) continue;
                    File.Copy(candidate, Path.Combine(themeDir, "back.png"), overwrite: true);
                    theme.BackgroundImagePath = Path.Combine(themeDir, "back.png");
                    break;
                }

                // Copy Setting.txt if present
                var settingSrc = Path.Combine(tempDir, "Setting.txt");
                if (File.Exists(settingSrc))
                    File.Copy(settingSrc, Path.Combine(themeDir, "Setting.txt"), overwrite: true);

                // Copy font folder if present
                var fontSrc = Path.Combine(tempDir, "font");
                var fontDst = Path.Combine(themeDir, "font");
                if (Directory.Exists(fontSrc))
                {
                    if (Directory.Exists(fontDst)) Directory.Delete(fontDst, recursive: true);
                    CopyDirectory(fontSrc, fontDst);
                }

                // Merge
                var existing = Themes.FirstOrDefault(t => t.Name == theme.Name);
                if (existing != null) Themes.Remove(existing);
                Themes.Add(theme);
                SaveToDataDir(theme);

                Logger.Info("Imported {0} from ZIP", theme.Name);
                return theme;
            }
            finally { try { Directory.Delete(tempDir, recursive: true); } catch { } }
        }
        catch (Exception ex) { Logger.Debug(ex, "ZIP import"); return null; }
    }

    private static void CopyDirectory(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var f in Directory.GetFiles(src)) File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), overwrite: true);
        foreach (var d in Directory.GetDirectories(src)) CopyDirectory(d, Path.Combine(dst, Path.GetFileName(d)));
    }

    public SoeyiTheme? ImportFromFile(string filePath)
    {
        // Handle ZIP imports
        if (filePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return ImportFromZip(filePath);

        try
        {
            var json = File.ReadAllText(filePath);
            var theme = JsonSerializer.Deserialize<SoeyiTheme>(json, JsonOpts);
            if (theme == null || string.IsNullOrEmpty(theme.Name))
                return null;

            // Check for accompanying assets (Programme format)
            var dir = Path.GetDirectoryName(filePath);
            if (dir != null)
            {
                theme.FolderPath = dir;
                var backPng = Path.Combine(dir, "back.png");
                if (File.Exists(backPng)) theme.BackgroundImagePath = backPng;
            }

            // Merge into themes list
            var existing = Themes.FirstOrDefault(t => t.Name == theme.Name);
            if (existing != null) Themes.Remove(existing);
            Themes.Add(theme);

            // Save a copy to our data dir
            SaveToDataDir(theme);

            Logger.Info("Imported theme: {0}", theme.Name);
            return theme;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Import failed: {0}", filePath);
            return null;
        }
    }

    public string? ExportToFile(SoeyiTheme theme, string? folderPath = null)
    {
        try
        {
            folderPath ??= _dataDir;
            Directory.CreateDirectory(folderPath);
            var filePath = Path.Combine(folderPath, $"{theme.Name}.json");
            var json = JsonSerializer.Serialize(theme, JsonOpts);
            File.WriteAllText(filePath, json);
            Logger.Info("Exported theme to: {0}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Export failed: {0}", theme.Name);
            return null;
        }
    }

    public void SaveTheme(SoeyiTheme theme)
    {
        SaveToDataDir(theme);

        // Also save Setting.txt if it exists in memory from editor
        if (!string.IsNullOrEmpty(theme.FolderPath))
        {
            var settings = GetThemeSettings(theme);
            if (settings.Count > 0)
            {
                var settingPath = Path.Combine(theme.FolderPath, "Setting.txt");
                var lines = new List<string>();
                foreach (var e in settings)
                {
                    var parts = new List<string> { e.Type + ":" };
                    if (e.Type == "Text" || e.Type == "BorderLine")
                    {
                        parts.Add($"x@{e.X}"); parts.Add($"y@{e.Y}"); parts.Add($"z@{e.Z}");
                        if (e.MaxHeight.HasValue) parts.Add($"maxheight@{e.MaxHeight}");
                        if (e.MaxWidth.HasValue) parts.Add($"maxwidth@{e.MaxWidth}");
                        if (!e.Visible) parts.Add("visible@false");
                        if (e.Centered) parts.Add("center@true");
                        if (!e.LabelVisible) parts.Add("labelVisible@false");

                        if (e.Type == "Text")
                        {
                            if (e.FontSize.HasValue) parts.Add($"FontSize@{e.FontSize}");
                            if (e.LabelFontSize.HasValue) parts.Add($"LabelFontSize@{e.LabelFontSize}");
                            if (e.FontFamily != null) parts.Add("FontFamily@" + e.FontFamily);
                            if (e.Foreground != null) parts.Add("Foreground@" + e.Foreground);
                            if (e.Data != null) parts.Add("data@" + e.Data);
                            if (e.Unit != null) parts.Add("unit@" + e.Unit);
                        }
                        else
                        {
                            if (e.Fill != null) parts.Add("Fill@" + e.Fill);
                            if (e.Data != null) parts.Add("data@" + e.Data);
                        }
                    }
                    lines.Add(string.Join(",", parts));
                }
                File.WriteAllText(settingPath, string.Join("\n", lines));
            }
        }

        Logger.Info("Saved theme: {0}", theme.Name);
    }

    public string? SaveAsNew(SoeyiTheme theme, string newName)
    {
        var clone = JsonSerializer.Deserialize<SoeyiTheme>(
            JsonSerializer.Serialize(theme, JsonOpts), JsonOpts)!;
        clone.Name = newName;
        clone.Type = 2; // DIY
        clone.IsDefault = false;

        // Create subfolder and copy assets (Setting.txt, bg image, fonts)
        var newFolder = Path.Combine(_dataDir, newName);
        Directory.CreateDirectory(newFolder);
        clone.FolderPath = newFolder;

        // Copy Setting.txt
        if (!string.IsNullOrEmpty(theme.FolderPath))
        {
            var srcSetting = Path.Combine(theme.FolderPath, "Setting.txt");
            if (File.Exists(srcSetting))
                File.Copy(srcSetting, Path.Combine(newFolder, "Setting.txt"), overwrite: true);

            // Copy background image
            var srcBg = Path.Combine(theme.FolderPath, "back.png");
            if (File.Exists(srcBg))
            {
                File.Copy(srcBg, Path.Combine(newFolder, "back.png"), overwrite: true);
                clone.BackgroundImagePath = Path.Combine(newFolder, "back.png");
            }

            // Copy fonts
            var srcFontDir = Path.Combine(theme.FolderPath, "font");
            if (Directory.Exists(srcFontDir))
            {
                var dstFontDir = Path.Combine(newFolder, "font");
                Directory.CreateDirectory(dstFontDir);
                foreach (var f in Directory.GetFiles(srcFontDir))
                    File.Copy(f, Path.Combine(dstFontDir, Path.GetFileName(f)), overwrite: true);
            }
        }

        var existing = Themes.FirstOrDefault(t => t.Name == newName);
        if (existing != null) Themes.Remove(existing);
        Themes.Add(clone);

        return ExportToFile(clone);
    }

    private void SaveToDataDir(SoeyiTheme theme)
    {
        var filePath = Path.Combine(_dataDir, $"{theme.Name}.json");
        var json = JsonSerializer.Serialize(theme, JsonOpts);
        File.WriteAllText(filePath, json);
    }

    public void AddNewTheme(SoeyiTheme theme)
    {
        // Keep existing folder path if already set (e.g. CropAndCreateTheme uses subfolder)
        if (string.IsNullOrEmpty(theme.FolderPath))
            theme.FolderPath = _dataDir;

        var existing = Themes.FirstOrDefault(t => t.Name == theme.Name);
        if (existing != null) Themes.Remove(existing);
        Themes.Add(theme);
        SaveToDataDir(theme);

        // Generate default Setting.txt for DIY themes so the editor works
        if (theme.Type == 2)
        {
            EnsureDefaultSettings(theme);
        }

        Logger.Info("Created new theme: {0}", theme.Name);
    }

    public void RemoveTheme(string name)
    {
        var theme = Themes.FirstOrDefault(t => t.Name == name);
        if (theme == null) return;
        if (theme.IsDefault) return;

        Themes.Remove(theme);
        var jsonPath = Path.Combine(_dataDir, $"{name}.json");
        if (File.Exists(jsonPath)) File.Delete(jsonPath);
        Logger.Info("Removed theme: {0}", name);
    }

    public void SetDefault(string name)
    {
        var theme = Themes.FirstOrDefault(t => t.Name == name);
        if (theme == null) return;

        // Unset previous default
        foreach (var t in Themes) t.IsDefault = false;

        theme.IsDefault = true;
        _config.Set("DefaultTheme", name);
        _config.Set("SelectedThemeName", name);
        _config.Save();
        Logger.Info("Set default theme: {0}", name);
    }

    private static void EnsureDefaultSettings(SoeyiTheme theme)
    {
        if (string.IsNullOrEmpty(theme.FolderPath)) return;
        var settingPath = Path.Combine(theme.FolderPath, "Setting.txt");
        if (File.Exists(settingPath)) return;

        // Default elements: CPU temp, CPU usage, GPU temp, GPU usage, RAM, Clock
        var lines = new List<string>
        {
            $"Text:x@{(int)(theme.Width*0.08)},y@{(int)(theme.Height*0.04)},z@10,maxwidth@{(int)(theme.Width*0.8)},FontSize@{(int)(theme.Height*0.04)},data@cpu_temp,unit@\u00B0C",
            $"Text:x@{(int)(theme.Width*0.08)},y@{(int)(theme.Height*0.10)},z@10,maxwidth@{(int)(theme.Width*0.8)},FontSize@{(int)(theme.Height*0.03)},data@cpu_usage,unit@%",
            $"Text:x@{(int)(theme.Width*0.08)},y@{(int)(theme.Height*0.15)},z@10,maxwidth@{(int)(theme.Width*0.8)},FontSize@{(int)(theme.Height*0.04)},data@gpu_temp,unit@\u00B0C",
            $"Text:x@{(int)(theme.Width*0.08)},y@{(int)(theme.Height*0.21)},z@10,maxwidth@{(int)(theme.Width*0.8)},FontSize@{(int)(theme.Height*0.03)},data@gpu_usage,unit@%",
            $"Text:x@{(int)(theme.Width*0.08)},y@{(int)(theme.Height*0.26)},z@10,maxwidth@{(int)(theme.Width*0.8)},FontSize@{(int)(theme.Height*0.03)},data@memory_usage,unit@G",
            $"Text:x@{(int)(theme.Width*0.08)},y@{(int)(theme.Height*0.90)},z@10,maxwidth@{(int)(theme.Width*0.8)},FontSize@{(int)(theme.Height*0.02)},data@time,unit@"
        };
        File.WriteAllText(settingPath, string.Join("\n", lines));
    }

    // ─── Setting.txt Parser (Programme themes) ────

    public static List<SettingElement> ParseSettingTxt(string folderPath)
    {
        var results = new List<SettingElement>();
        var settingPath = Path.Combine(folderPath, "Setting.txt");
        if (!File.Exists(settingPath)) return results;

        try
        {
            foreach (var line in File.ReadAllLines(settingPath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                var elem = ParseSettingLine(trimmed);
                if (elem != null) results.Add(elem);
            }
        }
        catch (Exception ex) { Logger.Warn(ex, "Parse Setting.txt failed"); }
        return results;
    }

    private static SettingElement? ParseSettingLine(string line)
    {
        try
        {
            var colon = line.IndexOf(':');
            if (colon < 0) return null;
            var type = line[..colon];
            var body = line[(colon + 1)..];
            var elem = new SettingElement { Type = type };
            var parts = body.Split(',');
            foreach (var part in parts)
            {
                var eq = part.IndexOf('@');
                if (eq < 0) continue;
                var key = part[..eq].Trim();
                var val = part[(eq + 1)..].Trim();
                switch (key)
                {
                    case "x": elem.X = double.Parse(val); break;
                    case "y": elem.Y = double.Parse(val); break;
                    case "z": elem.Z = int.Parse(val); break;
                    case "maxheight": elem.MaxHeight = double.Parse(val); break;
                    case "maxwidth": elem.MaxWidth = double.Parse(val); break;
                    case "FontSize": elem.FontSize = double.Parse(val); break;
                    case "LabelFontSize": elem.LabelFontSize = double.Parse(val); break;
                    case "visible": elem.Visible = !val.Equals("false", StringComparison.OrdinalIgnoreCase); break;
                    case "center": elem.Centered = val.Equals("true", StringComparison.OrdinalIgnoreCase); break;
                    case "labelVisible": elem.LabelVisible = !val.Equals("false", StringComparison.OrdinalIgnoreCase); break;
                    case "FontFamily": elem.FontFamily = val.TrimStart('#'); break;
                    case "Foreground": elem.Foreground = val; break;
                    case "Fill": elem.Fill = val; break;
                    case "Title": elem.Title = string.IsNullOrEmpty(val) ? null : val; break;
                    case "data": elem.Data = val; break;
                    case "unit": elem.Unit = val; break;
                    case "BorderThicknes": elem.BorderThickness = double.Parse(val); break;
                    case "BorderFill": elem.BorderFill = val; break;
                    case "BackColor": elem.BackColor = val; break;
                    case "CornerRadius": break; // decorative
                    case "height": if (type == "title.png" || type == "back.png")
                        elem.MaxHeight = double.Parse(val); break;
                    case "width": if (type == "title.png" || type == "back.png")
                        elem.MaxWidth = double.Parse(val); break;
                }
            }
            if (type.EndsWith(".png")) { elem.ImageFile = type; elem.Type = "Image"; }
            return elem;
        }
        catch { return null; }
    }

    public static List<SettingElement> GetThemeSettings(SoeyiTheme theme)
    {
        if (theme.FolderPath != null)
            return ParseSettingTxt(theme.FolderPath);
        return new List<SettingElement>();
    }
}
