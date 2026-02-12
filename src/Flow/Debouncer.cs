using System.Diagnostics;

namespace Shiron.Lib.Flow;

public class Debouncer {
    private readonly long _interval;
    private long _sinceLast;
    
    public Debouncer(long intervalMS) {
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
}
