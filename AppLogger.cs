using System.Text;

namespace PDFtoPS;

internal sealed class AppLogger
{
    private static readonly object Sync = new();
    private readonly string logDirectory;

    public AppLogger(string? baseDirectory = null)
    {
        logDirectory = ResolveWritableLogDirectory(baseDirectory);
    }

    public void Info(string message, params (string Key, object? Value)[] fields) => Write("INFO", message, fields);

    public void Warning(string message, params (string Key, object? Value)[] fields) => Write("WARN", message, fields);

    public void Error(string message, params (string Key, object? Value)[] fields) => Write("ERROR", message, fields);

    private void Write(string level, string message, params (string Key, object? Value)[] fields)
    {
        StringBuilder line = new();
        line.Append("ts=").Append(DateTime.Now.ToString("O"));
        line.Append(" level=").Append(level);
        line.Append(" msg=\"").Append(Escape(message)).Append('"');

        foreach ((string key, object? value) in fields)
        {
            line.Append(' ')
                .Append(key)
                .Append("=\"")
                .Append(Escape(value?.ToString() ?? string.Empty))
                .Append('"');
        }

        string filePath = Path.Combine(logDirectory, $"{DateTime.Now:yyyyMMdd}.log");

        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(logDirectory);
                File.AppendAllText(filePath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch
        {
            // Best-effort logging: never allow log I/O issues to break conversion flow.
        }
    }

    private static string ResolveWritableLogDirectory(string? baseDirectory)
    {
        if (!string.IsNullOrWhiteSpace(baseDirectory))
        {
            string explicitPath = Path.Combine(baseDirectory, "logs");
            if (TryEnsureDirectory(explicitPath)) return explicitPath;
        }

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            string preferred = Path.Combine(localAppData, "PDFtoPS", "logs");
            if (TryEnsureDirectory(preferred)) return preferred;
        }

        string appBase = Path.Combine(AppContext.BaseDirectory, "logs");
        if (TryEnsureDirectory(appBase)) return appBase;

        string tempFallback = Path.Combine(Path.GetTempPath(), "PDFtoPS", "logs");
        TryEnsureDirectory(tempFallback);
        return tempFallback;
    }

    private static bool TryEnsureDirectory(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string Escape(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace(Environment.NewLine, "\\n");
    }
}
