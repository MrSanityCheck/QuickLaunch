using Microsoft.Win32;

namespace QuickLaunch.Services;

public enum SystemTheme { Dark, Light }

public static class ThemeDetector
{
    public static SystemTheme Detect()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int value && value == 1)
                return SystemTheme.Light;
        }
        catch { }
        return SystemTheme.Dark;
    }
}
