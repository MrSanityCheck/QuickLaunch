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
            if (!path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                return IconFromPath(path, 0, NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_LARGEICON);

            var link = (IShellLinkW)new ShellLinkCoClass();
            ((IPersistFile)link).Load(path, 0);

            // Try file-system path first
            var sb = new System.Text.StringBuilder(260);
            link.GetPath(sb, 260, IntPtr.Zero, 0);
            var target = sb.ToString();

            if (!string.IsNullOrEmpty(target))
            {
                bool exists = File.Exists(target);
                logger?.Log($"Icon [{Path.GetFileName(path)}]: path=\"{target}\" exists={exists}");
                uint flags = NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_LARGEICON;
                uint attr  = 0;
                if (!exists) { flags |= NativeMethods.SHGFI_USEFILEATTRIBUTES; attr = NativeMethods.FILE_ATTRIBUTE_NORMAL; }
                return IconFromPath(target, attr, flags);
            }

            // No file-system path — use the target PIDL directly.
            // Works for network shares, virtual folders, and cloud items.
            link.GetIDList(out var pidl);
            if (pidl != IntPtr.Zero)
            {
                try
                {
                    logger?.Log($"Icon [{Path.GetFileName(path)}]: no path, resolving via PIDL");
                    return IconFromPidl(pidl);
                }
                finally { Marshal.FreeCoTaskMem(pidl); }
            }

            logger?.Log($"Icon [{Path.GetFileName(path)}]: no path and no PIDL — skipping");
            return null;
        }
        catch (Exception ex)
        {
            logger?.Log($"Icon failed [{Path.GetFileName(path)}]: {ex.Message}");
            return null;
        }
    }

    private static BitmapSource? IconFromPath(string path, uint dwAttr, uint flags)
    {
        var info   = new SHFILEINFO();
        var result = NativeMethods.SHGetFileInfo(path, dwAttr, ref info,
                         (uint)Marshal.SizeOf<SHFILEINFO>(), flags);
        if (result == IntPtr.Zero || info.hIcon == IntPtr.Zero) return null;
        try   { return BitmapSourceFromHIcon(info.hIcon); }
        finally { NativeMethods.DestroyIcon(info.hIcon); }
    }

    private static BitmapSource? IconFromPidl(IntPtr pidl)
    {
        var info   = new SHFILEINFO();
        var result = NativeMethods.SHGetFileInfo(pidl, 0, ref info,
                         (uint)Marshal.SizeOf<SHFILEINFO>(),
                         NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_LARGEICON | NativeMethods.SHGFI_PIDL);
        if (result == IntPtr.Zero || info.hIcon == IntPtr.Zero) return null;
        try   { return BitmapSourceFromHIcon(info.hIcon); }
        finally { NativeMethods.DestroyIcon(info.hIcon); }
    }

    private static BitmapSource BitmapSourceFromHIcon(IntPtr hIcon)
    {
        using var icon   = System.Drawing.Icon.FromHandle(hIcon);
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
        finally { NativeMethods.DeleteObject(hBitmap); }
    }
}
