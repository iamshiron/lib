using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shiron.Lib.Logging.Renderer;
using Shiron.Lib.Utils;

namespace Shiron.Lib.Logging;

/// <summary>Internal Manila logger.</summary>
public class Logger : ILogger, IContextualLogger {
    public UUID ContextID => new(0, 0);

    /// <summary>Injector map for add/remove operations.</summary>
    private readonly ConcurrentDictionary<UUID, LogInjector> _activeInjectors = new();
    /// <summary>Cached array snapshot for zero-allocation iteration. Updated on add/remove.</summary>
    private volatile LogInjector[] _injectorSnapshot = [];
    private readonly Lock _injectorLock = new();

    private readonly List<ILogRenderer> _renderers = [];
    /// <summary>Cached array snapshot for zero-allocation iteration.</summary>
    private volatile ILogRenderer[] _rendererSnapshot = [];
    private readonly Lock _rendererLock = new();

    private readonly List<ILogger> _sub = [];
    private readonly Logger? _parent = null;
    private readonly Logger? _rootLogger = null;

    public string? LoggerPrefix { get; }
    public readonly bool JsonLogger;
    private readonly JsonLogRenderer? _jsonRenderer;

    private readonly Stream _stdoutStream;

    public JsonLogRenderer? TempJsonRenderer => _jsonRenderer;

    public Logger(bool jsonLogger, Stream? stdoutStream = null) {
        LoggerPrefix = null;
        JsonLogger = jsonLogger;
        _stdoutStream = stdoutStream ?? Console.OpenStandardOutput();
        _jsonRenderer = jsonLogger ? new JsonLogRenderer(_stdoutStream) : null;
    }
    private Logger(string prefix, Logger parent, bool jsonLogger, Stream? stdoutStream = null) {
        LoggerPrefix = parent.LoggerPrefix != null ? $"{parent.LoggerPrefix}/{prefix}" : prefix;
        _parent = parent;
        JsonLogger = jsonLogger;
        _stdoutStream = stdoutStream ?? parent._stdoutStream;
        _jsonRenderer = jsonLogger ? new JsonLogRenderer(_stdoutStream) : null;

        // Find root logger by traversing up the parent chain.
        var current = parent;
        while (current._parent != null) {
            current = current._parent;
        }
        _rootLogger = current;
    }

    /// <inheritdoc/>
    public void AddInjector(UUID id, LogInjector injector) {
        lock (_injectorLock) {
            if (!_activeInjectors.TryAdd(id, injector)) {
                throw new Exception($"An injector with ID {id} already exists.");
            }
            // Update snapshot array for zero-allocation iteration
            _injectorSnapshot = [.. _activeInjectors.Values];
        }
    }

    /// <inheritdoc/>
    public void RemoveInjector(UUID id) {
        lock (_injectorLock) {
            if (!_activeInjectors.TryRemove(id, out _)) {
                throw new Exception($"No injector with ID {id} exists to remove.");
            }
            // Update snapshot array for zero-allocation iteration
            _injectorSnapshot = [.. _activeInjectors.Values];
        }
    }

    /// <inheritdoc/>
    public void RegisterConverters(IEnumerable<JsonConverter> converters) {
        _jsonRenderer?.RegisterConverters(converters);
    }

    /// <inheritdoc/>
    public void Log<T>(in LogPayload<T> payload) where T : notnull {
        var suppressLog = false;
        // Use cached array snapshot - array iteration uses struct enumerator, no boxing
        var injectors = _injectorSnapshot;
        for (var i = 0; i < injectors.Length; i++) {
            suppressLog |= injectors[i].Handle(payload);
        }

        if (suppressLog) {
            return;
        }

        var handled = false;
        var logger = this;
        while (true) {
            if (logger._jsonRenderer != null) {
                _ = logger._jsonRenderer!.RenderLog(payload, logger);
                handled = true;
                break;
            }

            var renderers = logger._rendererSnapshot;
            for (var i = 0; i < renderers.Length; i++) {
                if (renderers[i].RenderLog(payload, this)) {
                    handled = true;
                }
            }

            if (handled || logger._parent == null) {
                break;
            }
            logger = logger._parent;
        }

        if (!handled) {
            Warning($"Log entry was not handled by any renderer: {payload.Body.GetType().Name}");
        }
    }
    /// <inheritdoc/>
    public void Log<T>(in LogPayload<T> payload, out ContextualLogger logger) where T : notnull {
        var id = UUID.Random();
        logger = ContextualLogger.Create(this, id);
        Log(payload with { Header = payload.Header with { ContextID = id, ParentContextID = null } });
    }

    /// <inheritdoc/>
    public void Log<T>(LogLevel level, T entry) where T : notnull {
        Log(new LogPayload<T>(
            new LogHeader(level, LoggerPrefix, GetTimestamp(), null, null),
            entry
        ));
    }
    /// <inheritdoc/>
    public void Log<T>(LogLevel level, T entry, out ContextualLogger logger) where T : notnull {
        var id = UUID.Random();
        logger = ContextualLogger.Create(this, id);
        Log(new LogPayload<T>(
            new LogHeader(level, LoggerPrefix, GetTimestamp(), id, null),
            entry
        ));
    }

    /// <inheritdoc/>
    public void Log(LogLevel level, string message) {
        Log(new LogPayload<BasicLogEntry>(
            new LogHeader(level, LoggerPrefix, GetTimestamp(), null, null),
            new BasicLogEntry(message)
        ));
    }
    /// <inheritdoc/>
    public void Log(LogLevel level, string message, out ContextualLogger logger) {
        var id = UUID.Random();
        logger = ContextualLogger.Create(this, id);
        Log(new LogPayload<BasicLogEntry>(
            new LogHeader(level, LoggerPrefix, GetTimestamp(), id, null),
            new BasicLogEntry(message)
        ));
    }

    /// <inheritdoc/>
    public void MarkupLine(string message, LogLevel level) {
        Log(new LogPayload<MarkupLogEntry>(
            new LogHeader(level, LoggerPrefix, GetTimestamp(), ContextID, null),
            new MarkupLogEntry(message)
        ));
    }
    /// <inheritdoc/>
    public void MarkupLine(string message, LogLevel level, out ContextualLogger logger) {
        var id = UUID.Random();
        logger = ContextualLogger.Create(this, id);
        Log(new LogPayload<MarkupLogEntry>(
            new LogHeader(level, LoggerPrefix, GetTimestamp(), id, null),
            new MarkupLogEntry(message)
        ));
    }

    /// <inheritdoc/>
    public void Info(string message) {
        Log(LogLevel.Info, message);
    }
    /// <inheritdoc/>
    public void Info(string message, out ContextualLogger logger) {
        Log(LogLevel.Info, message, out logger);
    }

    /// <inheritdoc/>
    public void Debug(string message) {
        Log(LogLevel.Debug, message);
    }
    /// <inheritdoc/>
    public void Debug(string message, out ContextualLogger logger) {
        Log(LogLevel.Debug, message, out logger);
    }

    /// <inheritdoc/>
    public void Warning(string message) {
        Log(LogLevel.Warning, message);
    }
    /// <inheritdoc/>
    public void Warning(string message, out ContextualLogger logger) {
        Log(LogLevel.Warning, message, out logger);
    }

    /// <inheritdoc/>
    public void Error(string message) {
        Log(LogLevel.Error, message);
    }
    /// <inheritdoc/>
    public void Error(string message, out ContextualLogger logger) {
        Log(LogLevel.Error, message, out logger);
    }

    /// <inheritdoc/>
    public void Critical(string message) {
        Log(LogLevel.Critical, message);
    }
    /// <inheritdoc/>
    public void Critical(string message, out ContextualLogger logger) {
        Log(LogLevel.Critical, message, out logger);
    }

    /// <inheritdoc/>
    public void System(string message) {
        Log(LogLevel.System, message);
    }
    /// <inheritdoc/>
    public void System(string message, out ContextualLogger logger) {
        Log(LogLevel.System, message, out logger);
    }

    /// <inheritdoc/>
    public void AddRenderer(ILogRenderer renderer) {
        if (_jsonRenderer != null) {
            Warning("Cannot add renderers to a JSON logger.");
            return;
        }

        lock (_rendererLock) {
            _renderers.Add(renderer);
            _rendererSnapshot = [.. _renderers];
        }
    }

    /// <inheritdoc/>
    public ILogger CreateSubLogger(string prefix, bool jsonLogger = false, Stream? stdoutStream = null) {
        var sub = new Logger(prefix, this, jsonLogger, stdoutStream);
        _sub.Add(sub);

        return sub;
    }

    private static long GetTimestamp() {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}

public readonly struct ContextualLogger(IContextualLogger parent, UUID parentContextID, UUID contextID) : IContextualLogger {
    public string? LoggerPrefix => parent.LoggerPrefix;
    public UUID ContextID { get; } = contextID;
    private readonly UUID _parentContextID = parentContextID;

    public void Log<T>(in LogPayload<T> entry) where T : notnull {
        parent.Log(entry with { Header = entry.Header with { ContextID = ContextID, ParentContextID = _parentContextID } });
    }
    public void Log<T>(in LogPayload<T> entry, out ContextualLogger logger) where T : notnull {
        logger = Create(this, ContextID);
        parent.Log(entry with { Header = entry.Header with { ContextID = logger.ContextID, ParentContextID = ContextID } });
    }

    public void Log<T>(LogLevel level, T entry) where T : notnull {
        parent.Log(new LogPayload<T>(
            new LogHeader(level, parent.LoggerPrefix, GetTimestamp(), null, parent.ContextID),
            entry
        ));
    }
    public void Log<T>(LogLevel level, T entry, out ContextualLogger logger) where T : notnull {
        logger = Create(this, ContextID);
        parent.Log(new LogPayload<T>(
            new LogHeader(level, parent.LoggerPrefix, GetTimestamp(), logger.ContextID, ContextID),
            entry
        ));
    }

    public void Log(LogLevel level, string message) {
        Log(new LogPayload<BasicLogEntry>(
            new LogHeader(level, parent.LoggerPrefix, GetTimestamp(), null, parent.ContextID),
            new BasicLogEntry(message)
        ));
    }
    public void Log(LogLevel level, string message, out ContextualLogger logger) {
        logger = Create(this, ContextID);
        Log(new LogPayload<BasicLogEntry>(
            new LogHeader(level, parent.LoggerPrefix, GetTimestamp(), logger.ContextID, ContextID),
            new BasicLogEntry(message)
        ));
    }

    public void MarkupLine(string message, LogLevel level) {
        Log(new LogPayload<MarkupLogEntry>(
            new LogHeader(level, parent.LoggerPrefix, GetTimestamp(), null, parent.ContextID),
            new MarkupLogEntry(message)
        ));
    }
    public void MarkupLine(string message, LogLevel level, out ContextualLogger logger) {
        logger = Create(this, ContextID);
        Log(new LogPayload<MarkupLogEntry>(
            new LogHeader(level, parent.LoggerPrefix, GetTimestamp(), logger.ContextID, ContextID),
            new MarkupLogEntry(message)
        ));
    }

    public void Info(string message) {
        Log(LogLevel.Info, message);
    }
    public void Info(string message, out ContextualLogger logger) {
        Log(LogLevel.Info, message, out logger);
    }

    public void Debug(string message) {
        Log(LogLevel.Debug, message);
    }
    public void Debug(string message, out ContextualLogger logger) {
        Log(LogLevel.Debug, message, out logger);
    }

    public void Warning(string message) {
        Log(LogLevel.Warning, message);
    }
    public void Warning(string message, out ContextualLogger logger) {
        Log(LogLevel.Warning, message, out logger);
    }

    public void Error(string message) {
        Log(LogLevel.Error, message);
    }
    public void Error(string message, out ContextualLogger logger) {
        Log(LogLevel.Error, message, out logger);
    }

    public void Critical(string message) {
        Log(LogLevel.Critical, message);
    }
    public void Critical(string message, out ContextualLogger logger) {
        Log(LogLevel.Critical, message, out logger);
    }

    public void System(string message) {
        Log(LogLevel.System, message);
    }
    public void System(string message, out ContextualLogger logger) {
        Log(LogLevel.System, message, out logger);
    }

    private static long GetTimestamp() {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public static ContextualLogger Create(IContextualLogger logger, UUID parentContextID) {
        return new ContextualLogger(logger, parentContextID, UUID.Random());
    }
}
