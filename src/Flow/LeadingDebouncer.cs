using System.Diagnostics;

namespace Shiron.Lib.Flow;

/// <summary>
/// Provides a utility to manage debounce logic based on a specified silence period.
/// </summary>
/// <param name="silenceTimeMS">The time in milliseconds for the silence period.</param>
public class LeadingDebouncer(long silenceTimeMS) {
    private readonly long _silenceTicks = (long) (silenceTimeMS * (Stopwatch.Frequency / 1000.0));
    private long _lastTriggerTimestamp = Stopwatch.GetTimestamp();

    /// <summary>
    /// Attempts to execute based on debounce logic. Returns true if enough time has passed
    /// since the last trigger, false otherwise. Each call resets the debounce timer.
    /// </summary>
    /// <returns>
    /// True if the silence period has elapsed since the last trigger; false otherwise.
    /// </returns>
    public bool TryExecute() {
        var now = Stopwatch.GetTimestamp();
        var lastTrigger = Interlocked.Exchange(ref _lastTriggerTimestamp, now);
        var elapsed = now - lastTrigger;

        return elapsed >= _silenceTicks;
    }

    /// <summary>
    /// Resets the debouncer, clearing any pending debounce state.
    /// The next call to <see cref="TryExecute"/> will start a fresh silence period.
    /// </summary>
    public void Reset() {
        Interlocked.Exchange(ref _lastTriggerTimestamp, Stopwatch.GetTimestamp());
    }
}
