using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Shiron.Lib.Logging;

namespace Shiron.Lib.Profiling;

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
    /// Records an immediate event with the specified name and optional arguments.
    /// </summary>
    /// <param name="name">The name of the immediate event.</param>
    /// <param name="args">Optional arguments associated with the event.</param>
    void RecordImmediateEvent(string name, Dictionary<string, object>? args = null);

    /// <summary>
    /// Records a counter event with the specified name and value.
    /// </summary>
    /// <param name="name">The name of the counter event.</param>
    /// <param name="value">The value of the counter event.</param>
    void RecordCounter(string name, Dictionary<string, object> value);

    /// <summary>
    /// Gets the current timestamp in microseconds since the profiler started.
    /// </summary>
    long GetTimestamp();
}

/// <summary>
/// Provides a simple API for profiling code execution using the Chrome Trace Event Format.
/// Events are collected and can be saved to a JSON file for visualization in chrome://tracing.
/// </summary>
public class Profiler : IProfiler {
    private readonly ILogger? _logger;
    private readonly List<TraceEvent> _events = [];
    private readonly int _processId = Environment.ProcessId;
    private readonly Lock _lock = new();
    private readonly long _profilerStartTimestampMicroseconds = Utils.TimeNow();

    internal static readonly AsyncLocal<string?> _currentCategory = new();

    private static readonly JsonSerializerOptions _jsonOptions = new() {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public Profiler(ILogger? logger) {
        _logger = logger;
        RecordImmediateEvent("Profiler Start");
    }

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

        _logger?.Log(LogLevel.System, new ProfileBeingLogEntry(e.Name, e.Category, e.ProcessId, e.ThreadId, e.Timestamp));

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

        _logger?.Log(LogLevel.System, new ProfileCompleteLogEntry(e.Name, e.Category, e.ProcessId, e.ThreadId, e.Timestamp, 0));

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

        _logger?.Log(LogLevel.System, new ProfileCompleteLogEntry(e.Name, e.Category, e.ProcessId, e.ThreadId, e.Timestamp, e.Duration));

        lock (_lock) {
            _events.Add(e);
        }
    }

    /// <inheritdoc/>
    public void RecordCounter(string name, Dictionary<string, object> value) {
        var e = new TraceEvent {
            Name = name,
            Category = _currentCategory.Value ?? "default",
            Phase = "C",
            Timestamp = GetTimestamp(),
            ProcessId = _processId,
            ThreadId = Environment.CurrentManagedThreadId,
            Arguments = value
        };

        _logger?.Log(LogLevel.System, new ProfilingLogEntry(e.Name, e.Category, e.ProcessId, e.ThreadId, e.Timestamp));

        lock (_lock) {
            _events.Add(e);
        }
    }

    /// <inheritdoc/>
    public void RecordImmediateEvent(string name, Dictionary<string, object>? args = null) {
        var e = new TraceEvent {
            Name = name,
            Category = _currentCategory.Value ?? "default",
            Phase = "I",
            Timestamp = GetTimestamp(),
            ProcessId = _processId,
            ThreadId = Environment.CurrentManagedThreadId,
            Arguments = args ?? []
        };

        _logger?.Log(LogLevel.System, new ProfilingLogEntry(e.Name, e.Category, e.ProcessId, e.ThreadId, e.Timestamp));

        lock (_lock) {
            _events.Add(e);
        }
    }

    /// <inheritdoc/>
    public long GetTimestamp() {
        return Utils.TimeNow() - _profilerStartTimestampMicroseconds;
    }

    /// <summary>
    /// Clears all recorded profiling events.
    /// </summary>
    public void ClearEvents() {
        lock (_lock) {
            _events.Clear();
        }
    }

    /// <inheritdoc/>
    public string? SaveToFile(string baseDir) {
        if (!Directory.Exists(baseDir)) {
            return null;
        }

        lock (_lock) {
            RecordImmediateEvent("Profiler SaveToFile");

            string timestamp = DateTime.Now.ToString("yyyy-MM_dd-HH_mm_ss");
            string fileName = $"profile-{timestamp}.json";
            string filePath = Path.Combine(baseDir, fileName);

            string jsonString = JsonSerializer.Serialize(_events, _jsonOptions);
            File.WriteAllText(filePath, jsonString);
            return filePath;
        }
    }
}
