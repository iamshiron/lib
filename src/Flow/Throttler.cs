using System.Diagnostics;

namespace Shiron.Lib.Flow;

public class Throttler {
    private static double _tickFrequency = Stopwatch.Frequency / 1000.0;
    private readonly long _interval;
    private long _sinceLast;

    public Throttler(long intervalMS) {
        _interval = intervalMS * (Stopwatch.Frequency / 1000);
    }

    public void Reset() {
        _sinceLast = 0;
    }

    public bool TryDebounce() {
        var timePassed = Stopwatch.GetTimestamp() - _sinceLast;
        if (timePassed >= _interval) {
            _sinceLast = Stopwatch.GetTimestamp();
            return true;
        }
        return false;
    }

    public float CooldownProgress() {
        var elapsed = Stopwatch.GetTimestamp() - _sinceLast;
        return Math.Clamp((float) elapsed / _interval, 0f, 1f);
    }
    public float GetTimeRemainingMS() {
        var elapsed = Stopwatch.GetTimestamp() - _sinceLast;
        var remaining = _interval - elapsed;
        return remaining > 0 ? (float) (remaining / _tickFrequency) : 0;
    }
    public TimeSpan GetTimeRemaining() {
        var elapsed = Stopwatch.GetTimestamp() - _sinceLast;
        var remaining = _interval - elapsed;
        return remaining > 0 ? TimeSpan.FromTicks(remaining) : TimeSpan.Zero;
    }
}
