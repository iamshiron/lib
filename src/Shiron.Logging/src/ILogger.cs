
using Shiron.Logging.Renderer;

namespace Shiron.Logging;

/// <summary>Core logging interface.</summary>
public interface ILogger {
    /// <summary>Emit entry.</summary>
    /// <param name="entry">Log entry instance.</param>
    public void Log(ILogEntry entry);

    /// <summary>Emit markup info line.</summary>
    /// <param name="message">Message text.</param>
    /// <param name="logAlways">Bypass level filter.</param>
    public void MarkupLine(string message, bool logAlways = false);
    /// <summary>Info level.</summary>
    public void Info(string message);
    /// <summary>Debug level.</summary>
    public void Debug(string message);
    /// <summary>Warning level.</summary>
    public void Warning(string message);
    /// <summary>Error level.</summary>
    public void Error(string message);
    /// <summary>Critical level.</summary>
    public void Critical(string message);
    /// <summary>System level.</summary>
    public void System(string message);

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
    /// <returns>Sub-logger instance.</returns>
    public ILogger CreateSubLogger(string prefix);
}
