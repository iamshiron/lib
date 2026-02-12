using System.Diagnostics;

namespace Shiron.Lib.Flow;

public class LatchedThrottler {
    private static double _tickFrequency = Stopwatch.Frequency / 1000.0;
    private long _sinceLast;
    private readonly long _interval;
    private bool _isLatched;

    public LatchedThrottler(long intervalMS) {
        _interval = (long) (intervalMS * (Stopwatch.Frequency / 1000.0));
    }

    public void Signal() {
        _isLatched = true;
    }

    public bool Update() {
        var elapsed = Stopwatch.GetTimestamp() - _sinceLast;
        if (_isLatched && elapsed >= _interval) {
            _isLatched = false;
            _sinceLast = Stopwatch.GetTimestamp();
            return true;
        }
        return false;
    }

    public void Clear() {
        _isLatched = false;
    }
    public void Reset() {
        _sinceLast = 0;
    }
}
