using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
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
            // Resolve .lnk targets: SHGetFileInfo on the .lnk itself always adds
            // the shortcut overlay and misses Office IconHandlers (blank doc icons).
            var iconPath = path;
            uint dwAttr  = 0;
            uint flags   = NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_LARGEICON;

            if (path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                var target = ResolveTarget(path, logger);
                if (!string.IsNullOrEmpty(target))
                {
                    iconPath = target;
                    if (!File.Exists(target))
                    {
                        // Target not on disk — use extension-based icon without hitting the FS
                        dwAttr = NativeMethods.FILE_ATTRIBUTE_NORMAL;
                        flags |= NativeMethods.SHGFI_USEFILEATTRIBUTES;
                    }
                }
            }

            var info   = new SHFILEINFO();
            var result = NativeMethods.SHGetFileInfo(iconPath, dwAttr, ref info,
                             (uint)Marshal.SizeOf<SHFILEINFO>(), flags);

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
            logger?.Log($"Icon failed [{Path.GetFileName(path)}]: {ex.Message}");
            return null;
        }
    }

    private static string ResolveTarget(string lnkPath, Logger? logger)
    {
        // Try IShellLink first — handles more shortcut types than WScript.Shell
        try
        {
            var link = (IShellLinkW)new ShellLinkCoClass();
            ((IPersistFile)link).Load(lnkPath, 0);
            var sb = new System.Text.StringBuilder(260);
            link.GetPath(sb, 260, IntPtr.Zero, 0);
            var target = sb.ToString();
            if (!string.IsNullOrEmpty(target))
            {
                logger?.Log($"Icon [{Path.GetFileName(lnkPath)}]: IShellLink -> \"{target}\" exists={File.Exists(target)}");
                return target;
            }
        }
        catch { }

        // Fallback: WScript.Shell
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell", throwOnError: false);
            if (shellType == null) return "";
            var shell = Activator.CreateInstance(shellType);
            if (shell == null) return "";
            var shortcut = shellType.InvokeMember(
                "CreateShortcut", BindingFlags.InvokeMethod, null, shell, [lnkPath]);
            if (shortcut == null) return "";
            var target = shortcut.GetType().InvokeMember(
                "TargetPath", BindingFlags.GetProperty, null, shortcut, null) as string ?? "";
            logger?.Log($"Icon [{Path.GetFileName(lnkPath)}]: WScript.Shell -> \"{target}\" exists={File.Exists(target)}");
            return target;
        }
        catch { return ""; }
    }
}
