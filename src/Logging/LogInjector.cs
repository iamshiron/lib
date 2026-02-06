using System.Runtime.CompilerServices;

namespace Shiron.Lib.Logging;

public readonly record struct CapturedLog {
    public LogHeader Header { get; }
    public object? RawBody { get; }
    public string? Message { get; }

    public CapturedLog(LogHeader header, object body) {
        Header = header;
        RawBody = body;
    }
    public CapturedLog(LogHeader header, string message) {
        Header = header;
        Message = message;
    }
}

/// <summary>
/// Intercepts log entries from an <see cref="ILogger"/> to execute a callback, capture history, or suppress output.
/// <br/>
/// **WARNING**: Attaching a suppressed injector to a logger will prevent any renderers from receiving log entries while the injector is active.
/// </summary>
/// <param name="logger">Logger.</param>
/// <param name="onLog">Callback.</param>
/// <param name="filter">Entry predicate.</param>
/// <param name="suppressLogsDuringCapture">If true, log entries captured by this injector will not be passed to renderers.</param>
/// <param name="captureEntries">If true, captured entries are stored in CapturedEntries list. Set to false for suppression-only injectors to avoid allocations.</param>
public class LogInjector(ILogger logger, Action<CapturedLog>? onLog, Func<CapturedLog, bool>? filter, bool suppressLogsDuringCapture = false, bool captureEntries = true) : IDisposable {
    /// <summary>Injector ID.</summary>
    public readonly Guid ID = Guid.NewGuid();

    /// <summary>Callback handler (null for suppression-only injectors).</summary>
    internal readonly Action<CapturedLog>? _onLog = onLog;
    /// <summary>Optional filter.</summary>
    private readonly Func<CapturedLog, bool>? _filter = filter;
    /// <summary>Logger instance this injector is attached to.</summary>
    private readonly ILogger _logger = logger ?? throw new ArgumentException("No logger provided", nameof(logger));
    /// <summary>Suppress logs during capture.</summary>
    private readonly bool _suppressLogsDuringCapture = suppressLogsDuringCapture;
    /// <summary>Whether to capture entries to the list.</summary>
    private readonly bool _captureEntries = captureEntries;

    /// <summary>Returns whether the injector is currently injected.</summary>
    public bool IsInjected { get; private set; } = false;

    public List<CapturedLog> CapturedEntries { get; } = [];

    /// <summary>Ctor (no filter).</summary>
    /// <param name="logger">Logger.</param>
    /// <param name="onLog">Callback handler.</param>
    public LogInjector(ILogger logger, Action<CapturedLog> onLog, bool suppressLogsDuringCapture = false) : this(logger, onLog, null, suppressLogsDuringCapture, true) { }

    /// <summary>Creates a suppression-only injector that blocks log output without capturing or callbacks (zero allocations per log).</summary>
    /// <param name="logger">Logger.</param>
    /// <returns>Suppression-only injector.</returns>
    public static LogInjector CreateSuppressor(ILogger logger) => new(logger, null, null, true, false);

    public LogInjector Inject() {
        if (IsInjected) throw new InvalidOperationException("Injector is already injected");
        _logger.AddInjector(ID, this);
        IsInjected = true;
        return this;
    }

    /// <summary>Handle log entry.</summary>
    /// <param name="payload">Payload object.</param>
    internal bool Handle<T>(LogPayload<T> payload) where T : notnull {
        // Fast path: suppression-only injector with no callback, filter, or capture
        if (_onLog == null && _filter == null && !_captureEntries)
        {
            return _suppressLogsDuringCapture;
        }

        CapturedLog captured;
        if (typeof(T) == typeof(BasicLogEntry))
        {
            var temp = payload.Body;
            var basic = Unsafe.As<T, BasicLogEntry>(ref temp);
            captured = new CapturedLog(payload.Header, basic.Message);
        } else
        {
            captured = new CapturedLog(payload.Header, payload.Body);
        }

        if (_filter == null || _filter(captured))
        {
            if (_captureEntries)
            {
                CapturedEntries.Add(captured);
            }
            _onLog?.Invoke(captured);
            return _suppressLogsDuringCapture;
        }
        return false;
    }

    /// <summary>Detach injector, same result as calling <see cref="Dispose"/> directly.</summary>
    public void Eject() {
        Dispose();
    }

    /// <summary>Detach injector.</summary>
    public void Dispose() {
        if (!IsInjected)
            return; // Nothing to dispose if logger was never injected

        GC.SuppressFinalize(this);
        try
        {
            _logger.RemoveInjector(ID);
            IsInjected = false; // Allows for re-injection if needed
        } catch
        {
            // Swallow exceptions on dispose to avoid tearing down due to race conditions
            // (e.g., injector already removed or logger shutting down).
        }
    }

    /// <summary>Utility function to replay captured entries to another logger.</summary>
    /// <param name="logger">Logger instance.</param>
    public void Replay(ILogger logger) {
        foreach (var entry in CapturedEntries)
        {
            logger.Log(new LogPayload<CapturedLogEntry>(
                entry.Header,
                new CapturedLogEntry(entry.RawBody ?? entry.Message!)
            ));
        }
    }
}
