using Microsoft.Extensions.Logging;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 记录日志条目的测试替身，用于断言异常处理路径确实写了日志。
/// </summary>
internal sealed class RecordingLoggerProvider : ILoggerProvider
{
    private readonly List<LogEntry> _entries = [];

    /// <summary>
    /// 已记录的日志条目。
    /// </summary>
    public IReadOnlyList<LogEntry> Entries => _entries;

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) => new RecordingLogger(this, categoryName);

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    private void Add(LogEntry entry) => _entries.Add(entry);

    /// <summary>
    /// 一条被记录的日志。
    /// </summary>
    /// <param name="Category">日志类别。</param>
    /// <param name="Level">日志级别。</param>
    /// <param name="Message">格式化后的消息。</param>
    /// <param name="Exception">关联异常。</param>
    internal sealed record LogEntry(
        string Category,
        LogLevel Level,
        string Message,
        Exception? Exception
    );

    private sealed class RecordingLogger(RecordingLoggerProvider provider, string categoryName)
        : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            provider.Add(new LogEntry(categoryName, logLevel, formatter(state, exception), exception));
        }
    }
}
