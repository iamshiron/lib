namespace Shiron.Logging;

/// <summary>
/// Intercepts log entries from an <see cref="ILogger"/> to execute a callback, capture history, or suppress output.
/// <br/>
/// **WARNING**: Attaching a suppressed injector to a logger will prevent any renderers from receiving log entries while the injector is active.
/// </summary>
/// <param name="logger">Logger.</param>
/// <param name="onLog">Callback.</param>
/// <param name="filter">Entry predicate.</param>
/// <param name="suppressLogsDuringCapture">If true, log entries captured by this injector will not be passed to renderers.</param>
public class LogInjector(ILogger logger, Action<ILogEntry> onLog, Func<ILogEntry, bool>? filter, bool suppressLogsDuringCapture = false) : IDisposable {
    /// <summary>Injector ID.</summary>
    public readonly Guid ID = Guid.NewGuid();

    /// <summary>Callback handler.</summary>
    internal readonly Action<ILogEntry> _onLog = onLog ?? throw new ArgumentException("No callback provided", nameof(onLog));
    /// <summary>Optional filter.</summary>
    private readonly Func<ILogEntry, bool>? _filter = filter;
    /// <summary>Logger instance this injector is attached to.</summary>
    private readonly ILogger _logger = logger ?? throw new ArgumentException("No logger provided", nameof(logger));
    /// <summary>Suppress logs during capture.</summary>
    private readonly bool _suppressLogsDuringCapture = suppressLogsDuringCapture;

    /// <summary>Returns whether the injector is currently injected.</summary>
    public bool IsInjected { get; private set; } = false;

    public List<CapturedLogEntry> CapturedEntries { get; } = [];

    /// <summary>Ctor (no filter).</summary>
    /// <param name="logger">Logger.</param>
    /// <param name="onLog">Callback handler.</param>
    public LogInjector(ILogger logger, Action<ILogEntry> onLog, bool suppressLogsDuringCapture = false) : this(logger, onLog, null, suppressLogsDuringCapture) { }

    public LogInjector Inject() {
        if (IsInjected) throw new InvalidOperationException("Injector is already injected");
        _logger.AddInjector(ID, this);
        IsInjected = true;
        return this;
    }

    /// <summary>Handle log entry.</summary>
    /// <param name="entry">Entry object.</param>
    internal bool Handle(ILogEntry entry) {
        if (_filter == null || _filter(entry)) {
            CapturedEntries.Add(new CapturedLogEntry(entry));
            _onLog(entry);
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
        try {
            _logger.RemoveInjector(ID);
            IsInjected = false; // Allows for re-injection if needed
        } catch {
            // Swallow exceptions on dispose to avoid tearing down due to race conditions
            // (e.g., injector already removed or logger shutting down).
        }
    }

    /// <summary>Utility function to replay captured entries to another logger.</summary>
    /// <param name="logger">Logger instance.</param>
    public void Replay(ILogger logger) {
        foreach (var entry in CapturedEntries) {
            logger.Log(entry);
        }
    }
}
