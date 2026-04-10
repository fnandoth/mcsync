using System.Collections.Concurrent;

namespace MCSync.Core;

public sealed class AppLogger
{
    private readonly object _fileLock = new();
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public event EventHandler<LogEntry>? EntryWritten;

    public void Info(string message) => Write("INFO", message);

    public void Warning(string message) => Write("WARN", message);

    public void Error(string message, Exception? exception = null)
    {
        var fullMessage = exception is null ? message : $"{message}{Environment.NewLine}{exception}";
        Write("ERROR", fullMessage);
    }

    public IReadOnlyCollection<LogEntry> Snapshot() => _entries.ToArray();

    private void Write(string level, string message)
    {
        AppPaths.EnsureCreated();

        var entry = new LogEntry(DateTimeOffset.Now, level, message);
        _entries.Enqueue(entry);

        lock (_fileLock)
        {
            File.AppendAllLines(AppPaths.LogFilePath, [entry.ToString()]);
        }

        EntryWritten?.Invoke(this, entry);
    }
}

public sealed record LogEntry(DateTimeOffset Timestamp, string Level, string Message)
{
    public override string ToString() => $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Level}: {Message}";
}
