using System.IO;
using System.Threading;
using System.Windows;
using QuickLaunch.Services;
using QuickLaunch.Tray;
using Application = System.Windows.Application;
using Timer = System.Threading.Timer;

namespace QuickLaunch;

public partial class App : Application
{
    private const string MutexName = "QuickLaunch_SingleInstance_cdshaw";

    private Mutex? _mutex;
    private Logger? _logger;
    private TrayIconController? _tray;
    private FileSystemWatcher? _watcher;
    private Timer? _reloadDebounce;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(initiallyOwned: true, MutexName, out bool isNewInstance);
        if (!isNewInstance)
        {
            // Another instance is already running — exit silently.
            _mutex.Dispose();
            Shutdown();
            return;
        }

        _logger = new Logger(Path.Combine(AppContext.BaseDirectory, "Logs"));

        DispatcherUnhandledException += (_, ex) =>
        {
            _logger.Log($"Unhandled UI exception: {ex.Exception}");
            ex.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
        {
            _logger?.Log($"Fatal exception: {ex.ExceptionObject}");
        };

        var settings = AppSettings.Load(AppSettings.DefaultPath, _logger);
        _logger.Enabled = settings.EnableLogging;

        if (!File.Exists(AppSettings.DefaultPath))
            settings.Save(AppSettings.DefaultPath, _logger);

        _logger.Log($"QuickLaunch started (ShortcutFolder={settings.ShortcutFolder} IconSize={settings.IconSize} ShowLabels={settings.ShowLabels})");

        _tray = new TrayIconController(settings, _logger);
        _tray.ExitRequested += (_, _) =>
        {
            _logger.Log("QuickLaunch exiting");
            _tray.Dispose();
            Shutdown();
        };

        WatchSettings(settings, _logger);
    }

    private void WatchSettings(AppSettings settings, Logger logger)
    {
        var dir = Path.GetDirectoryName(AppSettings.DefaultPath)!;
        var file = Path.GetFileName(AppSettings.DefaultPath);

        if (!Directory.Exists(dir)) return;

        _watcher = new FileSystemWatcher(dir, file)
        {
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };

        _watcher.Changed += (_, _) =>
        {
            // Debounce: editors write files in multiple steps
            _reloadDebounce?.Dispose();
            _reloadDebounce = new Timer(_ =>
            {
                settings.Reload(AppSettings.DefaultPath, logger);
                _logger!.Enabled = settings.EnableLogging;
                logger.Log("Settings reloaded");
                Dispatcher.Invoke(() => _tray?.RefreshIcon());
            }, null, 300, Timeout.Infinite);
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _watcher?.Dispose();
        _reloadDebounce?.Dispose();
        _tray?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        _logger?.Dispose();
        base.OnExit(e);
    }
}
