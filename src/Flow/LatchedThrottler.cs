using System.Diagnostics;

namespace Shiron.Lib.Flow;

/// <summary>
/// Implements a throttling mechanism with a latch to control the execution of timed intervals.
/// </summary>
/// <param name="intervalMS">The interval in milliseconds for which operations should be throttled.</param>
public class LatchedThrottler(long intervalMS) {
    private readonly long _interval = (long) (intervalMS * (Stopwatch.Frequency / 1000.0));
    private static double _tickFrequency = Stopwatch.Frequency / 1000.0;
    private long _sinceLast;
    private bool _isLatched;

    /// <summary>
    /// Activates the latch, marking the throttler to trigger on the next valid interval.
    /// </summary>
    public void Trigger() {
        _isLatched = true;
    }

    /// <summary>
    /// Updates the throttler's state, determining if the latch should trigger based on the elapsed time and interval.
    /// </summary>
    /// <returns>True if the throttler triggers and the latch is reset; otherwise, false.</returns>
    public bool Update() {
        var elapsed = Stopwatch.GetTimestamp() - _sinceLast;
        if (_isLatched && elapsed >= _interval) {
            _isLatched = false;
            _sinceLast = Stopwatch.GetTimestamp();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Resets the latch to an inactive state.
    /// </summary>
    public void Clear() {
        _isLatched = false;
    }
    /// <summary>
    /// Resets the throttler's internal timing, clearing any elapsed time since the last trigger.
    /// </summary>
    public void Reset() {
        _sinceLast = 0;
    }
}
