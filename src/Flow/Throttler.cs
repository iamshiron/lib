using System.Diagnostics;

namespace Shiron.Lib.Flow;

/// <summary>
/// Provides throttling functionality to regulate the frequency of certain operations based on a specified interval.
/// </summary>
/// <param name="intervalMS">The interval in milliseconds for which operations should be throttled.</param>
public class Throttler(long intervalMS) {
    private static readonly double TickFrequency = Stopwatch.Frequency / 1000.0;
    private readonly long _interval = (long) (intervalMS * (Stopwatch.Frequency / 1000.0));
    private long _sinceLast;

    /// <summary>
    /// Resets the internal state of the throttler, allowing the regulated operation to start fresh.
    /// This sets the internal timestamp to zero, effectively removing any cooldown or timing restrictions
    /// until the next operation is performed.
    /// </summary>
    public void Reset() {
        _sinceLast = 0;
    }

    /// <summary>
    /// Attempts to execute the operation if the specified interval has elapsed since the last successful attempt.
    /// Updates the internal state to track the timestamp of the current operation if successful.
    /// </summary>
    /// <returns>
    /// True if the operation is allowed based on the throttling interval, otherwise false.
    /// </returns>
    public bool TryExecute() {
        var timePassed = Stopwatch.GetTimestamp() - _sinceLast;
        if (timePassed >= _interval) {
            _sinceLast = Stopwatch.GetTimestamp();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the progress of the cooldown as a value between 0 and 1, where 0 indicates no progress
    /// and 1 indicates the cooldown is complete.
    /// </summary>
    /// <returns>
    /// A float value between 0 and 1 representing the progress of the cooldown.
    /// </returns>
    public float CooldownProgress() {
        var elapsed = Stopwatch.GetTimestamp() - _sinceLast;
        return Math.Clamp((float) elapsed / _interval, 0f, 1f);
    }
    /// <summary>
    /// Calculates and returns the remaining time in milliseconds until the throttled operation can be performed again.
    /// If the interval has already elapsed, returns 0.
    /// </summary>
    /// <returns>
    /// Remaining time in milliseconds as a float, or 0 if the cooldown period has already been completed.
    /// </returns>
    public float GetTimeRemainingMS() {
        var elapsed = Stopwatch.GetTimestamp() - _sinceLast;
        var remaining = _interval - elapsed;
        return remaining > 0 ? (float) (remaining / TickFrequency) : 0;
    }
    /// <summary>
    /// Calculates the remaining time until the cooldown period is complete.
    /// </summary>
    /// <returns>
    /// A TimeSpan representing the time left for the cooldown to complete.
    /// Returns a TimeSpan of zero if the cooldown has already ended.
    /// </returns>
    public TimeSpan GetTimeRemaining() {
        var elapsed = Stopwatch.GetTimestamp() - _sinceLast;
        var remaining = _interval - elapsed;
        return remaining > 0 ? TimeSpan.FromTicks(remaining) : TimeSpan.Zero;
    }
}
