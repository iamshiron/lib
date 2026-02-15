using System.Text.Json.Serialization;
using Shiron.Lib.Logging.Renderer;
using Shiron.Lib.Utils;

namespace Shiron.Lib.Logging;

/// <summary>Core logging interface.</summary>
public interface ILogger : ILogFunctions {
    /// <summary> Register JSON converters for serialization. </summary>
    void RegisterConverters(IEnumerable<JsonConverter> converters);

    /// <summary>Add a log renderer.</summary>
    void AddRenderer(ILogRenderer renderer);

    /// <summary>Add injector.</summary>
    /// <param name="id">Injector ID.</param>
    /// <param name="injector">Injector instance.</param>
    void AddInjector(UUID id, LogInjector injector);
    /// <summary>Remove injector.</summary>
    /// <param name="id">Injector ID.</param>
    void RemoveInjector(UUID id);

    /// <summary>Create a sub-logger with a prefix.</summary>
    /// <param name="prefix">Prefix string.</param>
    /// <param name="jsonLogger">Whether to enable JSON logging. Parent setting is always prioritized.</param>
    /// <param name="stdoutStream">Optional custom output stream. If null, the parent's output stream is used.</param>
    /// <returns>Sub-logger instance.</returns>
    ILogger CreateSubLogger(string prefix, bool jsonLogger = false, Stream? stdoutStream = null);
}

public interface IContextualLogger : ILogFunctions {
    UUID ContextID { get; }

    /// <summary>Optional prefix.</summary>
    string? LoggerPrefix { get; }
}
