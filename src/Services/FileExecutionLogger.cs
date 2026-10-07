using System.Globalization;

namespace ElasticsearchMovieApi.Services;

public class FileExecutionLogger : IExecutionLogger
{
    private readonly object _lock = new();
    private readonly string _logFilePath;

    public FileExecutionLogger()
    {
        _logFilePath = Path.Combine(AppContext.BaseDirectory, "execution.log");
    }

    public void Log(string operation, TimeSpan elapsed)
    {
        var message = string.Format(
            CultureInfo.InvariantCulture,
            "{0:O} operation={1} elapsed_ms={2:F3}{3}",
            DateTimeOffset.UtcNow,
            operation,
            elapsed.TotalMilliseconds,
            Environment.NewLine);

        lock (_lock)
        {
            File.AppendAllText(_logFilePath, message);
        }
    }
}