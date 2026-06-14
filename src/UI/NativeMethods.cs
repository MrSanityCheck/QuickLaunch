using System.Runtime.InteropServices;

namespace QuickLaunch.UI;

[StructLayout(LayoutKind.Sequential)]
internal struct AccentPolicy
{
    public int AccentState;
    public int AccentFlags;
    public int GradientColor; // ABGR: (A << 24) | (B << 16) | (G << 8) | R
    public int AnimationId;
}

[StructLayout(LayoutKind.Sequential)]
internal struct WindowCompositionAttribData
{
    public int    Attribute;
    public IntPtr Data;
    public int    SizeOfData;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
internal struct SHFILEINFO
{
    public IntPtr hIcon;
    public int    iIcon;
    public uint   dwAttributes;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    public string szDisplayName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
    public string szTypeName;
}

internal static class NativeMethods
{
    [DllImport("user32.dll")]
    internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttribData data);

    [DllImport("dwmapi.dll")]
    internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("user32.dll")]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    internal static extern int SetWindowLong(IntPtr hwnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    internal static extern int GetWindowLong(IntPtr hwnd, int nIndex);

    [DllImport("gdi32.dll")]
    internal static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    internal static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    internal static extern IntPtr SHGetFileInfo(
        string pszPath, uint dwFileAttributes,
        ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

    internal const uint SHGFI_ICON      = 0x100;
    internal const uint SHGFI_LARGEICON = 0x000;

    internal const int WCA_ACCENT_POLICY               = 19;
    internal const int ACCENT_ENABLE_ACRYLICBLURBEHIND = 4;

    internal const int DWMWA_WINDOW_CORNER_PREFERENCE  = 33;
    internal const int DWMWCP_ROUND                    = 2;

    internal const int GWL_EXSTYLE      = -20;
    internal const int WS_EX_TOOLWINDOW = 0x00000080;
}
