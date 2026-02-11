using System.Collections.Concurrent;
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
    void RecordCompleteEvent(string name, long timestampMicroseconds, long durationMicroseconds, long? allocationCount = null, long? allocationSizeBytes = null, Dictionary<string, object>? args = null);

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
    void RecordCounter(string name, string counterName, long counterValue);

    /// <summary>
    /// Records allocation statistics with the specified name, allocation count, and allocation size in bytes.
    /// </summary>
    /// <param name="name">The name of the allocation event.</param>
    /// <param name="allocationCount">The number of allocations.</param>
    /// <param name="allocationSizeBytes">The total size of allocations in bytes.</param>
    void RecordAllocations(string name, long allocationSizeBytes);

    void Serialize(Utf8JsonWriter writer);

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
    private readonly ConcurrentBag<ProfilingEvent> _events = [];
    private readonly int _processId = Environment.ProcessId;
    private readonly long _profilerStartTimestampMicroseconds = Utils.TimeNow();

    internal static readonly AsyncLocal<string?> _currentCategory = new();

    /// <summary>
    /// Gets the collected trace events.
    /// Warning: The returned array is a snapshot and does not reflect events added after the call.
    /// </summary>
    public ProfilingEvent[] TraceEvents => [.. _events];

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
        var e = new ProfilingEvent {
            Name = name,
            Category = _currentCategory.Value ?? "global",
            EventType = ProfilingEventType.BeginEvent,
            Timestamp = GetTimestamp(),
            ProcessID = _processId,
            ThreadID = Environment.CurrentManagedThreadId,
            Args = args
        };

        _logger?.Log(LogLevel.System, new ProfileBeingLogEntry(e.Name, e.Category, e.ProcessID, e.ThreadID, e.Timestamp));

        _events.Add(e);
    }

    /// <inheritdoc/>
    public void EndEvent(string name, Dictionary<string, object>? args = null) {
        var e = new ProfilingEvent {
            Name = name,
            Category = _currentCategory.Value ?? "default",
            EventType = ProfilingEventType.EndEvent,
            Timestamp = GetTimestamp(),
            ProcessID = _processId,
            ThreadID = Environment.CurrentManagedThreadId,
            Args = args
        };

        _logger?.Log(LogLevel.System, new ProfileCompleteLogEntry(e.Name, e.Category, e.ProcessID, e.ThreadID, e.Timestamp, 0));
        _events.Add(e);
    }

    /// <inheritdoc/>
    public void RecordCompleteEvent(string name, long timestampMicroseconds, long durationMicroseconds, long? allocationCount = null, long? allocationSizeBytes = null, Dictionary<string, object>? args = null) {
        var e = new ProfilingEvent {
            Name = name,
            Category = _currentCategory.Value ?? "default",
            EventType = ProfilingEventType.CompleteEvent,
            Timestamp = timestampMicroseconds,
            Duration = durationMicroseconds,
            ProcessID = _processId,
            ThreadID = Environment.CurrentManagedThreadId,
            AllocationSizeBytes = allocationSizeBytes,
            Args = args
        };

        _logger?.Log(LogLevel.System, new ProfileCompleteLogEntry(e.Name, e.Category, e.ProcessID, e.ThreadID, e.Timestamp, e.Duration.Value));

        _events.Add(e);
    }

    /// <inheritdoc/>
    public void RecordCounter(string name, string counterName, long counterValue) {
        var e = new ProfilingEvent {
            Name = name,
            Category = _currentCategory.Value ?? "default",
            EventType = ProfilingEventType.CounterEvent,
            Timestamp = GetTimestamp(),
            ProcessID = _processId,
            ThreadID = Environment.CurrentManagedThreadId,
            CounterName = counterName,
            CounterValue = counterValue
        };

        _logger?.Log(LogLevel.System, new ProfilingLogEntry(e.Name, e.Category, e.ProcessID, e.ThreadID, e.Timestamp));

        _events.Add(e);
    }

    /// <inheritdoc/>
    public void RecordAllocations(string name, long allocationSizeBytes) {
        var e = new ProfilingEvent {
            Name = name,
            Category = _currentCategory.Value ?? "default",
            EventType = ProfilingEventType.CounterEvent,
            Timestamp = GetTimestamp(),
            ProcessID = _processId,
            ThreadID = Environment.CurrentManagedThreadId,
            AllocationSizeBytes = allocationSizeBytes
        };

        _logger?.Log(LogLevel.System, new ProfilingLogEntry(e.Name, e.Category, e.ProcessID, e.ThreadID, e.Timestamp));

        _events.Add(e);
    }

    /// <inheritdoc/>
    public void RecordImmediateEvent(string name, Dictionary<string, object>? args = null) {
        var e = new ProfilingEvent {
            Name = name,
            Category = _currentCategory.Value ?? "default",
            EventType = ProfilingEventType.ImmediateEvent,
            Timestamp = GetTimestamp(),
            ProcessID = _processId,
            ThreadID = Environment.CurrentManagedThreadId,
            Args = args
        };

        _logger?.Log(LogLevel.System, new ProfilingLogEntry(e.Name, e.Category, e.ProcessID, e.ThreadID, e.Timestamp));

        _events.Add(e);
    }

    /// <inheritdoc/>
    public long GetTimestamp() {
        return Utils.TimeNow() - _profilerStartTimestampMicroseconds;
    }

    /// <summary>
    /// Clears all recorded profiling events.
    /// </summary>
    public void ClearEvents() {
        _events.Clear();
    }

    /// <inheritdoc/>
    public string? SaveToFile(string baseDir) {
        if (!Directory.Exists(baseDir)) {
            return null;
        }

        RecordImmediateEvent("Profiler SaveToFile");

        string timestamp = DateTime.Now.ToString("yyyy-MM_dd-HH_mm_ss");
        string fileName = $"profile-{timestamp}.json";
        string filePath = Path.Combine(baseDir, fileName);

        var json = new Utf8JsonWriter(File.OpenWrite(filePath), new JsonWriterOptions {
            Indented = false
        });
        Serialize(json);
        json.Flush();
        json.Dispose();
        return filePath;
    }

    public void Serialize(Utf8JsonWriter writer) {
        writer.WriteStartArray();

        for (int i = 0; i < _events.Count; ++i) {
            _events.ElementAt(i).Serialize(writer, _jsonOptions);
        }

        writer.WriteEndArray();
    }
}
