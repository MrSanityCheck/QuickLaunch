using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using QuickLaunch.UI;

namespace QuickLaunch.Services;

public record ShortcutEntry(string DisplayName, string FullPath, BitmapSource? Icon);

public static class ShortcutService
{
    private static readonly string[] SupportedExtensions = ["*.lnk", "*.url", "*.exe"];

    // Key: (fullPath, lastWriteUtcTicks) — auto-invalidates when the file is replaced or updated
    private static readonly ConcurrentDictionary<(string, long), BitmapSource?> _iconCache = new();

    public static IReadOnlyList<ShortcutEntry> Load(string folder, Logger? logger = null)
    {
        var results = new List<ShortcutEntry>();
        if (!Directory.Exists(folder)) return results;

        var files = SupportedExtensions
            .SelectMany(ext => Directory.GetFiles(folder, ext))
            .OrderBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase);

        foreach (var path in files)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var icon = GetCachedIcon(path, logger);
            results.Add(new ShortcutEntry(name, path, icon));
        }
        return results;
    }

    private static BitmapSource? GetCachedIcon(string path, Logger? logger)
    {
        try
        {
            var ticks = File.GetLastWriteTimeUtc(path).Ticks;
            return _iconCache.GetOrAdd((path, ticks), _ => ExtractIcon(path, logger));
        }
        catch
        {
            return ExtractIcon(path, logger);
        }
    }

    private static BitmapSource? ExtractIcon(string path, Logger? logger)
    {
        try
        {
            var info = new SHFILEINFO();
            var result = NativeMethods.SHGetFileInfo(
                path, 0, ref info,
                (uint)Marshal.SizeOf<SHFILEINFO>(),
                NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_LARGEICON);

            if (result == IntPtr.Zero || info.hIcon == IntPtr.Zero) return null;

            try
            {
                using var icon   = System.Drawing.Icon.FromHandle(info.hIcon);
                using var bitmap = icon.ToBitmap();
                var hBitmap = bitmap.GetHbitmap();
                try
                {
                    var source = Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap, IntPtr.Zero, Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    source.Freeze();
                    return source;
                }
                finally
                {
                    NativeMethods.DeleteObject(hBitmap);
                }
            }
            finally
            {
                NativeMethods.DestroyIcon(info.hIcon);
            }
        }
        catch (Exception ex)
        {
            logger?.Log($"Failed to extract icon for {path}: {ex.Message}");
            return null;
        }
    }
}
