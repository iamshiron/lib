
using Shiron.Lib.Logging.Renderer;

namespace Shiron.Lib.Logging;

/// <summary>Core logging interface.</summary>
public interface ILogger {
    /// <summary>Emit entry.</summary>
    /// <param name="entry">Log entry instance.</param>
    public void Log<T>(in LogPayload<T> entry) where T : notnull;
    public void Log<T>(LogLevel level, T entry) where T : notnull;
    public void Log(LogLevel level, string message);

    /// <summary>Emit markup info line.</summary>
    /// <param name="message">Message text.</param>
    /// <param name="logAlways">Bypass level filter.</param>
    public void MarkupLine(string message, LogLevel level);
    /// <summary>Info level.</summary>
    public void Info(string message, Guid? parentContextID = null);
    /// <summary>Debug level.</summary>
    public void Debug(string message, Guid? parentContextID = null);
    /// <summary>Warning level.</summary>
    public void Warning(string message, Guid? parentContextID = null);
    /// <summary>Error level.</summary>
    public void Error(string message, Guid? parentContextID = null);
    /// <summary>Critical level.</summary>
    public void Critical(string message, Guid? parentContextID = null);
    /// <summary>System level.</summary>
    public void System(string message, Guid? parentContextID = null);

    /// <summary>Add a log renderer.</summary>
    public void AddRenderer(ILogRenderer renderer);

    /// <summary>Add injector.</summary>
    /// <param name="id">Injector ID.</param>
    /// <param name="injector">Injector instance.</param>
    public void AddInjector(Guid id, LogInjector injector);
    /// <summary>Remove injector.</summary>
    /// <param name="id">Injector ID.</param>
    public void RemoveInjector(Guid id);

    /// <summary>Optional prefix.</summary>
    public string? LoggerPrefix { get; }

    /// <summary>Context manager.</summary>
    public LogContext LogContext { get; }

    /// <summary>Create sub-logger with prefix.</summary>
    /// <param name="prefix">Prefix string.</param>
    /// <param name="jsonLogger">Whether to enable JSON logging. Parent setting is always prioritized.</param>
    /// <param name="stdoutStream">Optional custom output stream. If null, the parent's output stream is used.</param>
    /// <returns>Sub-logger instance.</returns>
    public ILogger CreateSubLogger(string prefix, bool jsonLogger = false, Stream? stdoutStream = null);
}
