namespace WomierDriver.Core.Logging;

public interface ILogger
{
    void Info(string message);

    void Warn(string message);

    void Error(string message, Exception? exception = null);
}

public sealed class NullLogger : ILogger
{
    public static NullLogger Instance { get; } = new();

    public void Info(string message)
    {
    }

    public void Warn(string message)
    {
    }

    public void Error(string message, Exception? exception = null)
    {
    }
}

/// <summary>Appends timestamped lines to a text file; rotates once the file grows past 1 MB.</summary>
public sealed class FileLogger : ILogger
{
    private const long MaxBytes = 1024 * 1024;

    private readonly string _path;
    private readonly object _lock = new();

    public FileLogger(string path)
    {
        _path = path;
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <summary>Raised for every line written, so the UI can show a live log.</summary>
    public event Action<string>? LineWritten;

    public void Info(string message) => Write("INFO", message);

    public void Warn(string message) => Write("WARN", message);

    public void Error(string message, Exception? exception = null)
        => Write("ERROR", exception is null ? message : $"{message}: {exception.GetType().Name}: {exception.Message}");

    private void Write(string level, string message)
    {
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        lock (_lock)
        {
            try
            {
                var info = new FileInfo(_path);
                if (info.Exists && info.Length > MaxBytes)
                {
                    File.Move(_path, _path + ".1", overwrite: true);
                }

                File.AppendAllText(_path, line + Environment.NewLine);
            }
            catch
            {
                // Logging must never take the driver down.
            }
        }

        LineWritten?.Invoke(line);
    }
}
