using System.IO;
using System.Text.Json;
using QuickLaunch.Services;

namespace QuickLaunch;

public class AppSettings
{
    public string ShortcutFolder { get; set; } = "";
    public string TrayIconPath { get; set; } = "";
    public int IconSize { get; set; } = 32;
    public bool ShowLabels { get; set; } = true;
    public double WindowOpacity { get; set; } = 0.85;
    public bool EnableLogging { get; set; } = true;

    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "QuickLaunch", "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static AppSettings Load(string path, Logger? logger = null)
    {
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? Defaults();
            }
        }
        catch (Exception ex)
        {
            logger?.Log($"Failed to load settings: {ex.Message}");
        }
        return Defaults();
    }

    public void Reload(string path, Logger? logger = null)
    {
        var fresh = Load(path, logger);
        ShortcutFolder  = fresh.ShortcutFolder;
        TrayIconPath    = fresh.TrayIconPath;
        IconSize        = fresh.IconSize;
        ShowLabels      = fresh.ShowLabels;
        WindowOpacity   = fresh.WindowOpacity;
        EnableLogging   = fresh.EnableLogging;
    }

    public void Save(string path, Logger? logger = null)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception ex)
        {
            logger?.Log($"Failed to save settings: {ex.Message}");
        }
    }

    private static AppSettings Defaults() => new()
    {
        ShortcutFolder = @"C:\CDS\Portable\CDS",
        TrayIconPath = "",
        IconSize = 32,
        ShowLabels = true,
        WindowOpacity = 0.85,
        EnableLogging = true
    };
}
