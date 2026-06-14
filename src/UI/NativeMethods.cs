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

    internal const int WCA_ACCENT_POLICY               = 19;
    internal const int ACCENT_ENABLE_ACRYLICBLURBEHIND = 4;

    internal const int DWMWA_WINDOW_CORNER_PREFERENCE  = 33;
    internal const int DWMWCP_ROUND                    = 2;

    internal const int GWL_EXSTYLE      = -20;
    internal const int WS_EX_TOOLWINDOW = 0x00000080;
}
