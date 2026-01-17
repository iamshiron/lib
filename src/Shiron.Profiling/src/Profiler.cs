using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Shiron.Logging;

namespace Shiron.Profiling;

/// <summary>
/// Defines the interface for a profiler that can record profiling events.
/// </summary>
public interface IProfiler {
    /// <summary>
    /// Begins a profiling event with the specified name and optional arguments.
    /// </summary>
    /// <param name="name">The name of the profiling event.</param>
    /// <param name="args">Optional arguments associated with the event.</param>
    void BeginEvent(string name, Dictionary<string, object>? args = null);
    /// <summary>
    /// Ends a profiling event with the specified name and optional arguments.
    /// </summary>
    /// <param name="name">The name of the profiling event.</param>
    /// <param name="args">Optional arguments associated with the event.</param>
    void EndEvent(string name, Dictionary<string, object>? args = null);
    /// <summary>
    /// Saves the collected profiling events to a JSON file in the specified base directory.
    /// The file is named with a timestamp to ensure uniqueness.
    /// </summary>
    /// <param name="baseDir">The base directory where the profile file will be saved.</param>
    /// <returns>The full path of the saved profile file, or null if the directory does not exist.</returns>
    string? SaveToFile(string baseDir);

    /// <summary>
    /// Records a complete event with the specified name, timestamp, duration, and optional arguments.
    /// </summary>
    /// <param name="name">The name of the complete event.</param>
    /// <param name="timestampMicroseconds">The starting timestamp of the event in microseconds.</param>
    /// <param name="durationMicroseconds">The duration of the event in microseconds.</param>
    /// <param name="args">Optional arguments associated with the event.</param>
    void RecordCompleteEvent(string name, long timestampMicroseconds, long durationMicroseconds, Dictionary<string, object>? args = null);

    /// <summary>
    /// Gets the current timestamp in microseconds since the profiler started.
    /// </summary>
    long GetTimestamp();
}

/// <summary>
/// Provides a simple API for profiling code execution using the Chrome Trace Event Format.
/// Events are collected and can be saved to a JSON file for visualization in chrome://tracing.
/// </summary>
public class Profiler(ILogger logger, bool logProfiling) : IProfiler {
    private readonly ILogger _logger = logger;
    private readonly bool _logProfiling = logProfiling;
    private readonly List<TraceEvent> _events = [];
    private readonly Stopwatch _stopwatch = new();
    private readonly int _processId = Environment.ProcessId;
    private readonly Lock _lock = new();
    private readonly long _profilerStartTimestampMicroseconds = Utils.TimeNow();

    internal static readonly AsyncLocal<string?> _currentCategory = new();

    private static readonly JsonSerializerOptions _jsonOptions = new() {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <inheritdoc/>
    public void BeginEvent(string name, Dictionary<string, object>? args = null) {
        var e = new TraceEvent {
            Name = name,
            Category = _currentCategory.Value ?? "default",
            Phase = "B", // Begin event
            Timestamp = GetTimestamp(),
            ProcessId = _processId,
            ThreadId = Environment.CurrentManagedThreadId, // More modern way to get ThreadId
            Arguments = args ?? []
        };

        if (_logProfiling) _logger.Log(new LogPayload<ProfileBeingLogEntry>(
            new LogHeader(LogLevel.Info,
                "Profiler",
                Utils.TimeNow(),
                null
            ),
            new ProfileBeingLogEntry(e.Name, e.Category, e.ProcessId, e.ThreadId, e.Timestamp)
        ));

        lock (_lock) {
            _events.Add(e);
        }
    }

    /// <inheritdoc/>
    public void EndEvent(string name, Dictionary<string, object>? args = null) {
        var e = new TraceEvent {
            Name = name,
            Category = _currentCategory.Value ?? "default",
            Phase = "E", // End event
            Timestamp = GetTimestamp(),
            ProcessId = _processId,
            ThreadId = Environment.CurrentManagedThreadId,
            Arguments = args ?? []
        };

        if (_logProfiling) _logger.Log(new LogPayload<ProfileCompleteLogEntry>(
            new LogHeader(LogLevel.Info,
                "Profiler",
                Utils.TimeNow(),
                null
            ),
            new ProfileCompleteLogEntry(e.Name, e.Category, e.ProcessId, e.ThreadId, e.Timestamp, 0)
        ));

        lock (_lock) {
            _events.Add(e);
        }
    }

    /// <inheritdoc/>
    public void RecordCompleteEvent(string name, long timestampMicroseconds, long durationMicroseconds, Dictionary<string, object>? args = null) {
        var e = new TraceEvent {
            Name = name,
            Category = _currentCategory.Value ?? "default",
            Phase = "X", // Complete event
            Timestamp = timestampMicroseconds,
            Duration = durationMicroseconds,
            ProcessId = _processId,
            ThreadId = Environment.CurrentManagedThreadId,
            Arguments = args ?? []
        };

        if (_logProfiling) _logger.Log(new LogPayload<ProfileCompleteLogEntry>(
            new LogHeader(LogLevel.Info,
                "Profiler",
                Utils.TimeNow(),
                null
            ),
            new ProfileCompleteLogEntry(e.Name, e.Category, e.ProcessId, e.ThreadId, e.Timestamp, e.Duration)
        ));

        lock (_lock) {
            _events.Add(e);
        }
    }

    /// <inheritdoc/>
    public long GetTimestamp() {
        // return _stopwatch.ElapsedTicks * 1_000_000 / Stopwatch.Frequency;
        return Utils.TimeNow() - _profilerStartTimestampMicroseconds;
    }

    /// <summary>
    /// Clears all recorded profiling events.
    /// </summary>
    public void ClearEvents() {
        lock (_lock) {
            _events.Clear();
            _stopwatch.Reset(); // Reset the stopwatch as well
        }
    }

    /// <inheritdoc/>
    public string? SaveToFile(string baseDir) {
        if (!Directory.Exists(baseDir)) {
            _logger.Debug($"Base directory does not exist: {baseDir}. Profile data will not be saved.");
            return null;
        }

        lock (_lock) {
            string timestamp = DateTime.Now.ToString("yyyy-MM_dd-HH_mm_ss");
            string fileName = $"profile-{timestamp}.json";
            string filePath = Path.Combine(baseDir, fileName);

            string jsonString = JsonSerializer.Serialize(_events, _jsonOptions);
            File.WriteAllText(filePath, jsonString);
            _logger.Debug($"Profiling data saved to: {filePath}");
            return filePath;
        }
    }
}

/// <summary>Profiler category scope.</summary>
public readonly struct ProfileCategory : IDisposable {
    private readonly string? _previousCategory;

    /// <summary>Creates a profiler category scope.</summary>
    /// <param name="categoryName">Category name.</param>
    public ProfileCategory(string categoryName) {
        _previousCategory = Profiler._currentCategory.Value;
        Profiler._currentCategory.Value = categoryName;
    }

    /// <inheritdoc/>
    public void Dispose() {
        Profiler._currentCategory.Value = _previousCategory;
    }
}

/// <summary>Profiler scope for automatic event timing.</summary>
public readonly struct ProfileScope : IDisposable {
    private readonly IProfiler _profiler;
    private readonly string _name;
    private readonly long _startTimestampMicroseconds;
    private readonly Dictionary<string, object>? _args;

    /// <summary>Creates a profiling scope with the caller member name as the event name.</summary>
    /// <param name="profiler">Profiler instance.</param>
    /// <param name="args">Optional arguments.</param>
    /// <param name="name">Caller member name.</param>
    public ProfileScope(IProfiler profiler, Dictionary<string, object>? args = null, [CallerMemberName] string name = "") {
        _profiler = profiler;
        _args = args;
        _name = name;
        _startTimestampMicroseconds = _profiler.GetTimestamp();

        _profiler.BeginEvent(_name, _args);
    }

    /// <summary>Creates a profiling scope with a specified name.</summary>
    /// <param name="profiler">Profiler instance.</param>
    /// <param name="name">Event name.</param>
    /// <param name="args">Optional arguments.</param>
    public ProfileScope(IProfiler profiler, string name, Dictionary<string, object>? args = null) {
        _profiler = profiler;
        _name = name;
        _args = args;
        _startTimestampMicroseconds = _profiler.GetTimestamp();

        _profiler.BeginEvent(_name, _args);
    }

    /// <summary>Creates a profiling scope with a specified MethodBase.</summary>
    /// <param name="profiler">Profiler instance.</param>
    /// <param name="func">MethodBase to derive the event name from.</param>
    /// <param name="args">Optional arguments.</param>
    public ProfileScope(IProfiler profiler, MethodBase func, Dictionary<string, object>? args = null) {
        _profiler = profiler;
        _name = $"{func.DeclaringType?.FullName ?? "Unknown"}.{func.Name}";
        _args = args;
        _startTimestampMicroseconds = _profiler.GetTimestamp();

        _profiler.BeginEvent(_name, _args);
    }

    /// <inheritdoc/>
    public void Dispose() {
        long endTimestampMicroseconds = _profiler.GetTimestamp();
        long durationMicroseconds = endTimestampMicroseconds - _startTimestampMicroseconds;

        _profiler.EndEvent(_name, _args);
        _profiler.RecordCompleteEvent(_name, _startTimestampMicroseconds, durationMicroseconds, _args);
    }
}
