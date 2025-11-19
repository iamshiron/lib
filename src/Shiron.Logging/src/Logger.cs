using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using Shiron.Logging.Renderer;

namespace Shiron.Logging;

/// <summary>Async-local context GUID stack.</summary>
public class LogContext {
    /// <summary>Async-local stack.</summary>
    private readonly AsyncLocal<Stack<Guid>> _contextStack = new();

    public Guid? CurrentContextID => _contextStack.Value?.Count > 0 ? _contextStack.Value.Peek() : null;

    /// <summary>Push context.</summary>
    /// <param name="contextId">Context GUID.</param>
    /// <returns>Disposable restorer.</returns>
    public IDisposable PushContext(Guid contextId) {
        var originalStack = _contextStack.Value;
        var newStack = originalStack == null
            ? new Stack<Guid>()
            : new Stack<Guid>(originalStack.Reverse());

        newStack.Push(contextId);
        _contextStack.Value = newStack;
        return new ContextRestorer(this, originalStack ?? new Stack<Guid>());
    }

    /// <summary>Generate + push GUID.</summary>
    /// <param name="contextID">Generated GUID.</param>
    /// <returns>Disposable restorer.</returns>
    public IDisposable PushContext(out Guid contextID) { contextID = Guid.NewGuid(); return PushContext(contextID); }

    /// <summary>Restorer disposable.</summary>
    private sealed class ContextRestorer(LogContext context, Stack<Guid> stackToRestore) : IDisposable {
        private readonly LogContext _owner = context;
        private readonly Stack<Guid> _stackToRestore = stackToRestore;

        /// <summary>Restore stack.</summary>
        public void Dispose() { _owner._contextStack.Value = _stackToRestore; }
    }
}

/// <summary>Internal Manila logger.</summary>
public class Logger(string? prefix) : ILogger {
    /// <summary>Injector map.</summary>
    private readonly ConcurrentDictionary<Guid, LogInjector> _activeInjectors = new();
    private readonly List<ILogRenderer> _renderers = [];
    private readonly List<ILogger> _sub = [];
    private readonly Logger? _parrent = null;
    private readonly Logger? _rootLogger = null;
    public LogContext LogContext { get; } = new();

    public string? LoggerPrefix { get; } = prefix;

    private Logger(string prefix, Logger parent) : this(prefix) {
        _parrent = parent;

        // Find root logger by traversing up the parent chain.
        var current = parent;
        while (current._parrent != null) {
            current = current._parrent;
        }
        _rootLogger = current;
    }

    /// <inheritdoc/>
    public void AddInjector(Guid id, LogInjector injector) {
        if (!_activeInjectors.TryAdd(id, injector)) {
            throw new Exception($"An injector with ID {id} already exists.");
        }
    }

    /// <inheritdoc/>
    public void RemoveInjector(Guid id) {
        if (!_activeInjectors.TryRemove(id, out _)) {
            // Note: Depending on requirements, this could fail silently instead of throwing.
            // Throwing makes behavior more explicit.
            throw new Exception($"No injector with ID {id} exists to remove.");
        }
    }

    /// <inheritdoc/>
    public void Log(ILogEntry entry) {
        if (entry.ParentContextID == null && LogContext.CurrentContextID != null)
            entry.ParentContextID = LogContext.CurrentContextID;


        var handled = false;
        var logger = this;
        while (true) {
            foreach (var renderer in logger._renderers) {
                if (renderer.RenderLog(entry)) {
                    handled = true;
                }
            }

            if (handled || logger._parrent == null) break;
            logger = logger._parrent;
        }

        if (!handled)
            Warning($"Log entry was not handled by any renderer: {entry.GetType().Name}");

        // Iterate over a thread-safe snapshot of injectors. ConcurrentDictionary supports safe enumeration.
        foreach (var injector in _activeInjectors.Values) {
            injector.Handle(entry);
        }
    }

    /// <inheritdoc/>
    public void MarkupLine(string message, bool logAlways = false) {
        Log(new MarkupLogEntry(message, logAlways, LoggerPrefix));
    }

    /// <inheritdoc/>
    public void Info(string message) {
        Log(new BasicLogEntry(message, LogLevel.Info, LoggerPrefix));
    }

    /// <inheritdoc/>
    public void Debug(string message) {
        Log(new BasicLogEntry(message, LogLevel.Debug, LoggerPrefix));
    }

    /// <inheritdoc/>
    public void Warning(string message) {
        Log(new BasicLogEntry(message, LogLevel.Warning, LoggerPrefix));
    }

    /// <inheritdoc/>
    public void Error(string message) {
        Log(new BasicLogEntry(message, LogLevel.Error, LoggerPrefix));
    }

    /// <inheritdoc/>
    public void Critical(string message) {
        Log(new BasicLogEntry(message, LogLevel.Critical, LoggerPrefix));
    }

    /// <inheritdoc/>
    public void System(string message) {
        Log(new BasicLogEntry(message, LogLevel.System, LoggerPrefix));
    }

    /// <inheritdoc/>
    public void AddRenderer(ILogRenderer renderer) {
        _renderers.Add(renderer);
    }

    /// <inheritdoc/>
    public ILogger CreateSubLogger(string prefix) {
        var combinedPrefix = LoggerPrefix != null ? $"{LoggerPrefix}/{prefix}" : prefix;
        var sub = new Logger(combinedPrefix, this);
        _sub.Add(sub);

        return sub;
    }
}

/// <summary>Logger extensions.</summary>
public static class LoggerExtensions {
    /// <summary>Create context injector.</summary>
    /// <param name="logger">Logger.</param>
    /// <param name="onLog">Callback.</param>
    /// <param name="contextId">Context GUID.</param>
    /// <returns>Injector instance.</returns>
    public static LogInjector CreateContextInjector(this ILogger logger, Action<ILogEntry> onLog, Guid contextId) => new(logger, onLog, entry => entry.ParentContextID == contextId);
}
