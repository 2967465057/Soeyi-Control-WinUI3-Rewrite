using System.Text.Json.Serialization;

namespace SoeyiWinUI_v2.Models;

/// <summary>Fully compatible with original SOEYI theme JSON schema.</summary>
public class SoeyiTheme
{
    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("Type")]
    public int Type { get; set; } // 1=built-in, 2=DIY

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("Width")]
    public double Width { get; set; }

    [JsonPropertyName("Height")]
    public double Height { get; set; }

    [JsonPropertyName("FillMode")]
    public int FillMode { get; set; }

    [JsonPropertyName("CropArea")]
    public string CropArea { get; set; } = "0, 0, 0, 0";

    [JsonPropertyName("ThumbnailImageData")]
    public string? ThumbnailImageData { get; set; }

    [JsonPropertyName("Isup")]
    public bool IsUp { get; set; }

    [JsonPropertyName("IsTouch")]
    public int IsTouch { get; set; }

    [JsonPropertyName("IsApp")]
    public int IsApp { get; set; }

    [JsonPropertyName("DisplayDirection")]
    public int DisplayDirection { get; set; }

    [JsonPropertyName("FontDirection")]
    public int FontDirection { get; set; }

    [JsonPropertyName("BackgroundImage")]
    public string? BackgroundImage { get; set; }

    [JsonPropertyName("BackgroundVideoFile")]
    public string? BackgroundVideoFile { get; set; }

    [JsonPropertyName("DisplayTexts")]
    public List<ThemeTextElement> DisplayTexts { get; set; } = new();

    [JsonPropertyName("DisplayImages")]
    public List<ThemeImageElement> DisplayImages { get; set; } = new();

    [JsonPropertyName("TouchPoints")]
    public List<ThemeTouchPoint> TouchPoints { get; set; } = new();

    [JsonPropertyName("ShutDownCancelPoint")]
    public ThemeTouchPoint ShutDownCancelPoint { get; set; } = new();

    // ─── Runtime extras (not in JSON) ───
    [JsonIgnore]
    public string? FolderPath { get; set; }

    [JsonIgnore]
    public string? BackgroundImagePath { get; set; }
}

public class ThemeTextElement
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("Left")]
    public double Left { get; set; }

    [JsonPropertyName("Top")]
    public double Top { get; set; }

    [JsonPropertyName("ZIndex")]
    public int ZIndex { get; set; }

    [JsonPropertyName("TextType")]
    public string TextType { get; set; } = "";

    [JsonPropertyName("Text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("Color")]
    public string Color { get; set; } = "White";

    [JsonPropertyName("FontName")]
    public string FontName { get; set; } = "Microsoft YaHei UI";

    [JsonPropertyName("FontSize")]
    public double FontSize { get; set; } = 30;

    [JsonPropertyName("Bold")]
    public bool Bold { get; set; }

    [JsonPropertyName("Italic")]
    public bool Italic { get; set; }

    [JsonPropertyName("Underline")]
    public bool Underline { get; set; }
}

public class ThemeImageElement
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("Left")]
    public double Left { get; set; }

    [JsonPropertyName("Top")]
    public double Top { get; set; }

    [JsonPropertyName("ZIndex")]
    public int ZIndex { get; set; }

    [JsonPropertyName("Width")]
    public double Width { get; set; }

    [JsonPropertyName("Height")]
    public double Height { get; set; }

    [JsonPropertyName("ImagePath")]
    public string? ImagePath { get; set; }
}

public class ThemeTouchPoint
{
    [JsonPropertyName("TPType")]
    public int TPType { get; set; }

    [JsonPropertyName("IconPath")]
    public string? IconPath { get; set; }

    [JsonPropertyName("Content")]
    public string? Content { get; set; }

    [JsonPropertyName("X1")]
    public double X1 { get; set; }

    [JsonPropertyName("X2")]
    public double X2 { get; set; }

    [JsonPropertyName("Y1")]
    public double Y1 { get; set; }

    [JsonPropertyName("Y2")]
    public double Y2 { get; set; }
}

/// <summary>Parsed Setting.txt element (Programme themes).</summary>
public class SettingElement
{
    public string Type { get; set; } = "";    // Text, BorderLine, Image
    public double X { get; set; }
    public double Y { get; set; }
    public int Z { get; set; }
    public double? MaxHeight { get; set; }
    public double? MaxWidth { get; set; }
    public double? FontSize { get; set; }
    public double? LabelFontSize { get; set; }
    public string? FontFamily { get; set; }
    public string? Foreground { get; set; }
    public string? Fill { get; set; }
    public string? Title { get; set; }
    public string? Data { get; set; }        // CpuUsage, GpuUsage, MemoryUsage, CurrentDates, etc.
    public string? Unit { get; set; }
    public double? BorderThickness { get; set; }
    public string? BorderFill { get; set; }
    public string? BackColor { get; set; }
    public string? ImageFile { get; set; }
    public bool Visible { get; set; } = true;
    public bool Centered { get; set; }
    public bool LabelVisible { get; set; } = true;
}
