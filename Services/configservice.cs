using NLog;
using System.Text.Json;

namespace SoeyiWinUI_v2.Services;

public class ConfigService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly string _configDir;
    private readonly string _configPath;
    private Dictionary<string, object> _settings = new();

    public ConfigService()
    {
        _configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SoeyiWinUI");
        _configPath = Path.Combine(_configDir, "config.json");
        Load();
    }

    private void Load()
    {
        try
        {
            Directory.CreateDirectory(_configDir);
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                _settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? CreateDefaults();
                Logger.Info("Configuration loaded from {0}", _configPath);
            }
            else
            {
                _settings = CreateDefaults();
                Save();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load config");
            _settings = CreateDefaults();
        }
    }

    public T? Get<T>(string key, T? defaultValue = default)
    {
        if (_settings.TryGetValue(key, out var value) && value is JsonElement el)
        {
            try { return JsonSerializer.Deserialize<T>(el.GetRawText()); }
            catch { }
        }
        return value is T t ? t : defaultValue;
    }

    public void Set<T>(string key, T value) => _settings[key] = value!;

    public void Save()
    {
        var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }

    public string ConfigDirectory => _configDir;
    public string ThemeDirectory => Path.Combine(_configDir, "Themes");

    private static Dictionary<string, object> CreateDefaults() => new()
    {
        ["StartMinimized"] = false,
        ["AutoStart"] = false,
        ["CompressionQuality"] = 85,
        ["SelectedTheme"] = "default",
        ["Language"] = "zh-CN",
        ["ShowHardwareInfo"] = true,
        ["MonitorIntervalMs"] = 1000,
    };
}

