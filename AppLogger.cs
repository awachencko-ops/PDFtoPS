using System.Text;

namespace PDFtoPS;

internal sealed class AppLogger
{
    private static readonly object Sync = new();
    private readonly string logDirectory;

    public AppLogger(string? baseDirectory = null)
    {
        string root = baseDirectory ?? AppContext.BaseDirectory;
        logDirectory = Path.Combine(root, "logs");
        Directory.CreateDirectory(logDirectory);
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
        lock (Sync)
        {
            File.AppendAllText(filePath, line + Environment.NewLine, Encoding.UTF8);
        }
    }

    private static string Escape(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace(Environment.NewLine, "\\n");
    }
}
