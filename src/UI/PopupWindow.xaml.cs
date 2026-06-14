using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using QuickLaunch.Services;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace QuickLaunch.UI;

public partial class PopupWindow : Window
{
    private readonly AppSettings _settings;
    private readonly Logger _logger;
    private bool _activated;

    public PopupWindow(AppSettings settings, IReadOnlyList<ShortcutEntry> shortcuts, SystemTheme theme, Logger logger)
    {
        _settings = settings;
        _logger = logger;

        InitializeComponent();
        ApplyTheme(theme);
        PopulateShortcuts(shortcuts);
        Loaded += OnLoaded;
    }

    // ── Theme ────────────────────────────────────────────────────────────────

    private void ApplyTheme(SystemTheme theme)
    {
        var fg = theme == SystemTheme.Light
            ? Color.FromRgb(0x00, 0x00, 0x00)
            : Color.FromRgb(0xFF, 0xFF, 0xFF);

        var hover = theme == SystemTheme.Light
            ? Color.FromArgb(0x1A, 0x00, 0x00, 0x00)
            : Color.FromArgb(0x2A, 0xFF, 0xFF, 0xFF);

        Resources["ForegroundBrush"] = new SolidColorBrush(fg);
        Resources["HoverBrush"]      = new SolidColorBrush(hover);
        _theme = theme;
    }

    private SystemTheme _theme;

    // ── Shortcuts ─────────────────────────────────────────────────────────────

    private void PopulateShortcuts(IReadOnlyList<ShortcutEntry> shortcuts)
    {
        if (shortcuts.Count == 0)
        {
            EmptyMessage.Text = System.IO.Directory.Exists(_settings.ShortcutFolder)
                ? "No shortcuts found."
                : $"Shortcut folder not found:\n{_settings.ShortcutFolder}";
            EmptyMessage.Visibility = Visibility.Visible;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            timer.Tick += (_, _) => { timer.Stop(); Close(); };
            timer.Start();
            return;
        }

        ShortcutList.ItemsSource = shortcuts.Select(s => new ShortcutViewModel(s, _settings)).ToList();
    }

    private void ShortcutButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: string path })
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                _logger.Log($"Launched: {path}");
            }
            catch (Exception ex)
            {
                _logger.Log($"Failed to launch {path}: {ex.Message}");
            }
            Close();
        }
    }

    // ── Window lifetime ───────────────────────────────────────────────────────

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        _activated = true;
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        if (_activated) Close();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;

        // Remove taskbar button
        var exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, exStyle | NativeMethods.WS_EX_TOOLWINDOW);

        // Rounded corners (Windows 11 21H2+) — clips both the acrylic and WPF content
        int cornerPref = NativeMethods.DWMWCP_ROUND;
        NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE,
            ref cornerPref, Marshal.SizeOf(cornerPref));

        // Acrylic blur via SetWindowCompositionAttribute — works with AllowsTransparency layered windows.
        // GradientColor is ABGR: alpha controls tint strength, colour matches system theme.
        byte alpha = (byte)Math.Clamp(_settings.WindowOpacity * 255, 0, 255);
        int gradientColor = _theme == SystemTheme.Light
            ? (alpha << 24) | 0xF3F3F3
            : (alpha << 24) | 0x202020;

        var accent = new AccentPolicy
        {
            AccentState   = NativeMethods.ACCENT_ENABLE_ACRYLICBLURBEHIND,
            GradientColor = gradientColor
        };

        int size = Marshal.SizeOf(accent);
        var ptr  = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(accent, ptr, false);
            var data = new WindowCompositionAttribData
            {
                Attribute  = NativeMethods.WCA_ACCENT_POLICY,
                Data       = ptr,
                SizeOfData = size
            };
            NativeMethods.SetWindowCompositionAttribute(hwnd, ref data);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PositionAboveTray();
        Activate();
    }

    // ── Positioning ───────────────────────────────────────────────────────────

    private void PositionAboveTray()
    {
        var cursorPhysical = System.Windows.Forms.Cursor.Position;
        var screen = System.Windows.Forms.Screen.FromPoint(cursorPhysical);

        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget == null) return;

        double scaleX = source.CompositionTarget.TransformToDevice.M11;
        double scaleY = source.CompositionTarget.TransformToDevice.M22;

        double logicalLeft   = screen.WorkingArea.Left   / scaleX;
        double logicalRight  = screen.WorkingArea.Right  / scaleX;
        double logicalTop    = screen.WorkingArea.Top    / scaleY;
        double logicalBottom = screen.WorkingArea.Bottom / scaleY;
        double logicalMouseX = cursorPhysical.X          / scaleX;

        Left = Math.Max(logicalLeft,
               Math.Min(logicalMouseX, logicalRight - ActualWidth));

        Top = Math.Max(logicalTop, logicalBottom - ActualHeight - 5);

        _logger.Log($"Popup positioned: Left={Left:F0} Top={Top:F0} " +
                    $"W={ActualWidth:F0} H={ActualHeight:F0} scale={scaleX:F2}");
    }
}

// ── View model ────────────────────────────────────────────────────────────────

file sealed class ShortcutViewModel
{
    public string DisplayName { get; }
    public string FullPath    { get; }
    public System.Windows.Media.ImageSource? Icon { get; }
    public double IconSize    { get; }
    public bool   ShowLabel   { get; }

    public ShortcutViewModel(ShortcutEntry entry, AppSettings settings)
    {
        DisplayName = entry.DisplayName;
        FullPath    = entry.FullPath;
        Icon        = entry.Icon;
        IconSize    = settings.IconSize;
        ShowLabel   = settings.ShowLabels;
    }
}
