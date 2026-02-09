using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Shiron.Lib.Profiling;

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
    private readonly long _startAllocationSizeBytes;
    private readonly Dictionary<string, object>? _args;

    /// <summary>Creates a profiling scope with a specified name. This is the Primary Constructor.</summary>
    /// <param name="profiler">Profiler instance.</param>
    /// <param name="name">Event name.</param>
    /// <param name="args">Optional arguments.</param>
    public ProfileScope(IProfiler profiler, string name, Dictionary<string, object>? args = null) {
        _profiler = profiler;
        _name = name;
        _args = args;
        _startTimestampMicroseconds = _profiler.GetTimestamp();
        _startAllocationSizeBytes = GC.GetTotalAllocatedBytes();

        _profiler.RecordAllocations("Memory", _startAllocationSizeBytes);
    }

    /// <summary>Creates a profiling scope with the caller member name as the event name.</summary>
    /// <param name="profiler">Profiler instance.</param>
    /// <param name="args">Optional arguments.</param>
    /// <param name="name">Caller member name.</param>
    public ProfileScope(IProfiler profiler, Dictionary<string, object>? args = null, [CallerMemberName] string name = "")
        : this(profiler, name, args) {
    }

    /// <summary>Creates a profiling scope with a specified MethodBase.</summary>
    /// <param name="profiler">Profiler instance.</param>
    /// <param name="func">MethodBase to derive the event name from.</param>
    /// <param name="args">Optional arguments.</param>
    public ProfileScope(IProfiler profiler, MethodBase func, Dictionary<string, object>? args = null)
        : this(profiler, $"{func.DeclaringType?.FullName ?? "Unknown"}.{func.Name}", args) {
    }

    /// <inheritdoc/>
    public void Dispose() {
        long endTimestampMicroseconds = _profiler.GetTimestamp();
        // Ensure duration is at least 1 microsecond to avoid zero-duration events
        if (endTimestampMicroseconds <= _startTimestampMicroseconds)
        {
            endTimestampMicroseconds = _startTimestampMicroseconds + 1;
        }

        long durationMicroseconds = endTimestampMicroseconds - _startTimestampMicroseconds;
        long endAllocationSizeBytes = GC.GetTotalAllocatedBytes();

        _profiler.RecordCompleteEvent(_name, _startTimestampMicroseconds, durationMicroseconds);
        _profiler.RecordAllocations("Memory", endAllocationSizeBytes);
    }
}
