using System.IO;

namespace QuickLaunch.Services;

public sealed class Logger : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly object _lock = new();

    public Logger(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);
        var path = Path.Combine(logDirectory, "QuickLaunch.log");
        var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        _writer = new StreamWriter(stream, System.Text.Encoding.UTF8) { AutoFlush = true };
    }

    public bool Enabled { get; set; } = true;

    public void Log(string message)
    {
        if (!Enabled) return;
        lock (_lock)
        {
            try { _writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}"); }
            catch { }
        }
    }

    public void Dispose() => _writer.Dispose();
}
