namespace Shiron.Lib.Logging;

public interface ILogFunctions {
    void Log<T>(in LogPayload<T> entry) where T : notnull;
    void Log<T>(LogLevel level, T entry) where T : notnull;
    void Log(LogLevel level, string message);

    /// <summary>Emit markup info line.</summary>
    void MarkupLine(string message, LogLevel level);
    /// <summary>Info level.</summary>
    void Info(string message);
    /// <summary>Debug level.</summary>
    void Debug(string message);
    /// <summary>Warning level.</summary>
    void Warning(string message);
    /// <summary>Error level.</summary>
    void Error(string message);
    /// <summary>Critical level.</summary>
    void Critical(string message);
    /// <summary>System level.</summary>
    void System(string message);

    // Contextual logging functions
    void Log<T>(in LogPayload<T> entry, out ContextualLogger logger) where T : notnull;
    void Log<T>(LogLevel level, T entry, out ContextualLogger logger) where T : notnull;
    void Log(LogLevel level, string message, out ContextualLogger logger);

    /// <summary>Emit markup info line.</summary>
    void MarkupLine(string message, LogLevel level, out ContextualLogger logger);
    /// <summary>Info level.</summary>
    void Info(string message, out ContextualLogger logger);
    /// <summary>Debug level.</summary>
    void Debug(string message, out ContextualLogger logger);
    /// <summary>Warning level.</summary>
    void Warning(string message, out ContextualLogger logger);
    /// <summary>Error level.</summary>
    void Error(string message, out ContextualLogger logger);
    /// <summary>Critical level.</summary>
    void Critical(string message, out ContextualLogger logger);
    /// <summary>System level.</summary>
    void System(string message, out ContextualLogger logger);
}
