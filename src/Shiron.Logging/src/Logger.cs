using System.Collections.Concurrent;
using Shiron.Logging.Renderer;

namespace Shiron.Logging;

/// <summary>Immutable linked list node for context stack (avoids allocations on traversal).</summary>
internal sealed class ContextNode(Guid id, ContextNode? parent) {
    public readonly Guid Id = id;
    public readonly ContextNode? Parent = parent;
}

/// <summary>Async-local context GUID stack.</summary>
public class LogContext {
    /// <summary>Async-local stack using immutable linked list.</summary>
    private readonly AsyncLocal<ContextNode?> _contextStack = new();

    public Guid? CurrentContextID => _contextStack.Value?.Id;

    /// <summary>Push context.</summary>
    /// <param name="contextId">Context GUID.</param>
    /// <returns>Disposable restorer.</returns>
    public ContextRestorer PushContext(Guid contextId) {
        var original = _contextStack.Value;
        _contextStack.Value = new ContextNode(contextId, original);
        return new ContextRestorer(this, original);
    }

    /// <summary>Generate + push GUID.</summary>
    /// <param name="contextID">Generated GUID.</param>
    /// <returns>Disposable restorer.</returns>
    public ContextRestorer PushContext(out Guid contextID) { contextID = Guid.NewGuid(); return PushContext(contextID); }

    /// <summary>Restorer struct - avoids heap allocation when used with 'using' statement.</summary>
    public readonly struct ContextRestorer : IDisposable {
        private readonly LogContext _owner;
        private readonly ContextNode? _nodeToRestore;

        internal ContextRestorer(LogContext context, ContextNode? nodeToRestore) {
            _owner = context;
            _nodeToRestore = nodeToRestore;
        }

        /// <summary>Restore stack.</summary>
        public void Dispose() => _owner._contextStack.Value = _nodeToRestore;
    }
}

/// <summary>Internal Manila logger.</summary>
public class Logger(string? prefix) : ILogger {
    /// <summary>Injector map for add/remove operations.</summary>
    private readonly ConcurrentDictionary<Guid, LogInjector> _activeInjectors = new();
    /// <summary>Cached array snapshot for zero-allocation iteration. Updated on add/remove.</summary>
    private volatile LogInjector[] _injectorSnapshot = [];
    private readonly object _injectorLock = new();

    private readonly List<ILogRenderer> _renderers = [];
    /// <summary>Cached array snapshot for zero-allocation iteration.</summary>
    private volatile ILogRenderer[] _rendererSnapshot = [];
    private readonly object _rendererLock = new();

    private readonly List<ILogger> _sub = [];
    private readonly Logger? _parent = null;
    private readonly Logger? _rootLogger = null;
    public LogContext LogContext { get; } = new();

    public string? LoggerPrefix { get; } = prefix;

    private Logger(string prefix, Logger parent) : this(prefix) {
        _parent = parent;

        // Find root logger by traversing up the parent chain.
        var current = parent;
        while (current._parent != null) {
            current = current._parent;
        }
        _rootLogger = current;
    }

    /// <inheritdoc/>
    public void AddInjector(Guid id, LogInjector injector) {
        lock (_injectorLock) {
            if (!_activeInjectors.TryAdd(id, injector)) {
                throw new Exception($"An injector with ID {id} already exists.");
            }
            // Update snapshot array for zero-allocation iteration
            _injectorSnapshot = [.. _activeInjectors.Values];
        }
    }

    /// <inheritdoc/>
    public void RemoveInjector(Guid id) {
        lock (_injectorLock) {
            if (!_activeInjectors.TryRemove(id, out _)) {
                throw new Exception($"No injector with ID {id} exists to remove.");
            }
            // Update snapshot array for zero-allocation iteration
            _injectorSnapshot = [.. _activeInjectors.Values];
        }
    }

    /// <inheritdoc/>
    public void Log<T>(in LogPayload<T> payload) where T : notnull {
        var suppressLog = false;
        // Use cached array snapshot - array iteration uses struct enumerator, no boxing
        var injectors = _injectorSnapshot;
        for (var i = 0; i < injectors.Length; i++) {
            suppressLog |= injectors[i].Handle(payload);
        }

        if (suppressLog)
            return;

        var handled = false;
        var logger = this;
        while (true) {
            // Use cached array snapshot - array iteration is zero-allocation
            var renderers = logger._rendererSnapshot;
            for (var i = 0; i < renderers.Length; i++) {
                if (renderers[i].RenderLog(payload)) {
                    handled = true;
                }
            }

            if (handled || logger._parent == null) break;
            logger = logger._parent;
        }

        if (!handled)
            Warning($"Log entry was not handled by any renderer: {payload.Body.GetType().Name}");
    }
    public void Log<T>(LogLevel level, T entry) where T : notnull =>
        Log(new LogPayload<T>(
            new LogHeader(level, LoggerPrefix, GetTimestamp(), LogContext.CurrentContextID),
            entry
        ));
    public void Log(LogLevel level, string message) =>
        Log(new LogPayload<BasicLogEntry>(
            new LogHeader(level, LoggerPrefix, GetTimestamp(), LogContext.CurrentContextID),
            new BasicLogEntry(message)
        ));

    /// <inheritdoc/>
    public void MarkupLine(string message, LogLevel level) =>
        Log(new LogPayload<MarkupLogEntry>(
            new LogHeader(level, LoggerPrefix, GetTimestamp(), LogContext.CurrentContextID),
            new MarkupLogEntry(message)
        ));

    /// <inheritdoc/>
    public void Info(string message, Guid? parentContextID = null) =>
        Log(new LogPayload<BasicLogEntry>(
            new LogHeader(LogLevel.Info, LoggerPrefix, GetTimestamp(), LogContext.CurrentContextID),
            new BasicLogEntry(message)
        ));

    /// <inheritdoc/>
    public void Debug(string message, Guid? parentContextID = null) =>
        Log(new LogPayload<BasicLogEntry>(
            new LogHeader(LogLevel.Debug, LoggerPrefix, GetTimestamp(), LogContext.CurrentContextID),
            new BasicLogEntry(message)
        ));

    /// <inheritdoc/>
    public void Warning(string message, Guid? parentContextID = null) =>
        Log(new LogPayload<BasicLogEntry>(
            new LogHeader(LogLevel.Warning, LoggerPrefix, GetTimestamp(), LogContext.CurrentContextID),
            new BasicLogEntry(message)
        ));

    /// <inheritdoc/>
    public void Error(string message, Guid? parentContextID = null) =>
        Log(new LogPayload<BasicLogEntry>(
            new LogHeader(LogLevel.Error, LoggerPrefix, GetTimestamp(), LogContext.CurrentContextID),
            new BasicLogEntry(message)
        ));

    /// <inheritdoc/>
    public void Critical(string message, Guid? parentContextID = null) =>
        Log(new LogPayload<BasicLogEntry>(
            new LogHeader(LogLevel.Critical, LoggerPrefix, GetTimestamp(), LogContext.CurrentContextID),
            new BasicLogEntry(message)
        ));

    /// <inheritdoc/>
    public void System(string message, Guid? parentContextID = null) =>
        Log(new LogPayload<BasicLogEntry>(
            new LogHeader(LogLevel.System, LoggerPrefix, GetTimestamp(), LogContext.CurrentContextID),
            new BasicLogEntry(message)
        ));

    /// <inheritdoc/>
    public void AddRenderer(ILogRenderer renderer) {
        lock (_rendererLock) {
            _renderers.Add(renderer);
            _rendererSnapshot = [.. _renderers];
        }
    }

    /// <inheritdoc/>
    public ILogger CreateSubLogger(string prefix) {
        var combinedPrefix = LoggerPrefix != null ? $"{LoggerPrefix}/{prefix}" : prefix;
        var sub = new Logger(combinedPrefix, this);
        _sub.Add(sub);

        return sub;
    }

    private static long GetTimestamp() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

/// <summary>Logger extensions.</summary>
public static class LoggerExtensions {
    /// <summary>Create context injector.</summary>
    /// <param name="logger">Logger.</param>
    /// <param name="onLog">Callback.</param>
    /// <param name="contextId">Context GUID.</param>
    /// <returns>Injector instance.</returns>
    public static LogInjector CreateContextInjector(this ILogger logger, Action<CapturedLog> onLog) => new(logger, onLog, true);
}
