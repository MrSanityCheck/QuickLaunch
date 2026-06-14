using System.IO;
using Microsoft.Win32;
using QuickLaunch.Services;
using QuickLaunch.UI;
using Application = System.Windows.Application;

namespace QuickLaunch.Tray;

public sealed class TrayIconController : IDisposable
{
    private readonly AppSettings _settings;
    private readonly Logger _logger;
    private readonly System.Windows.Forms.NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.ContextMenuStrip _contextMenu;

    public event EventHandler? ExitRequested;

    public TrayIconController(AppSettings settings, Logger logger)
    {
        _settings = settings;
        _logger = logger;

        _contextMenu = BuildContextMenu();

        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Text = "Quick Launch",
            Visible = true,
            ContextMenuStrip = _contextMenu   // WinForms handles right-click: shows on mouse-up, DPI-aware
        };
        _notifyIcon.Icon = LoadIcon();
        _notifyIcon.MouseDown += OnMouseDown;

        SystemEvents.PowerModeChanged += OnPowerModeChanged;

        EnsureShortcutFolder();
    }

    // ── Tray icon ─────────────────────────────────────────────────────────────

    private System.Drawing.Icon LoadIcon()
    {
        var baseDir = AppContext.BaseDirectory;

        if (!string.IsNullOrEmpty(_settings.TrayIconPath) && File.Exists(_settings.TrayIconPath))
            return new System.Drawing.Icon(_settings.TrayIconPath);

        var defaultIcon = Path.Combine(baseDir, "icons", "icon.ico");
        if (File.Exists(defaultIcon))
            return new System.Drawing.Icon(defaultIcon);

        return System.Drawing.SystemIcons.Application;
    }

    private void EnsureShortcutFolder()
    {
        try
        {
            if (!Directory.Exists(_settings.ShortcutFolder))
            {
                Directory.CreateDirectory(_settings.ShortcutFolder);
                _logger.Log($"Created shortcut folder: {_settings.ShortcutFolder}");
            }
        }
        catch (Exception ex)
        {
            _logger.Log($"Failed to create shortcut folder: {ex.Message}");
        }
    }

    public void RefreshIcon() => _notifyIcon.Icon = LoadIcon();

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            // Re-register the tray icon — Windows drops it on sleep/resume
            _notifyIcon.Visible = false;
            _notifyIcon.Visible = true;
            _logger.Log("Power resume: tray icon refreshed");
        }
    }

    // ── Mouse handling ────────────────────────────────────────────────────────

    private void OnMouseDown(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (e.Button == System.Windows.Forms.MouseButtons.Left)
        {
            Application.Current.Dispatcher.Invoke(ShowPopup);
        }
    }

    private void ShowPopup()
    {
        var theme = ThemeDetector.Detect();
        var shortcuts = ShortcutService.Load(_settings.ShortcutFolder, _logger);

        var popup = new PopupWindow(_settings, shortcuts, theme, _logger);
        popup.Show();
    }

    // ── Context menu ──────────────────────────────────────────────────────────

    private System.Windows.Forms.ContextMenuStrip BuildContextMenu()
    {
        var menu = new System.Windows.Forms.ContextMenuStrip();

        var config = new System.Windows.Forms.ToolStripMenuItem("Config");
        config.Click += (_, _) =>
        {
            try
            {
                if (Directory.Exists(_settings.ShortcutFolder))
                    System.Diagnostics.Process.Start("explorer.exe", _settings.ShortcutFolder);
            }
            catch (Exception ex) { _logger.Log($"Config open failed: {ex.Message}"); }
        };

        var editSettings = new System.Windows.Forms.ToolStripMenuItem("Edit Settings");
        editSettings.Click += (_, _) =>
        {
            var path = AppSettings.DefaultPath;
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
                _logger.Log($"Opened settings: {path}");
            }
            catch (Exception ex) { _logger.Log($"Failed to open settings: {ex.Message}"); }
        };

        var quit = new System.Windows.Forms.ToolStripMenuItem("Quit");
        quit.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

        menu.Items.Add(config);
        menu.Items.Add(editSettings);
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add(quit);

        return menu;
    }

    // ── Disposal ──────────────────────────────────────────────────────────────

    public void Dispose()
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
    }
}
