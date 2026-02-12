using System.Diagnostics;

namespace Shiron.Lib.Flow;

public class TrailingDebouncer(long silenceTimeMS) {
    private readonly long _silenceTicks = (long) (silenceTimeMS * (Stopwatch.Frequency / 1000.0));
    private long _lastSignal;
    private bool _pending;

    public void Signal() {
        _lastSignal = Stopwatch.GetTimestamp();
        _pending = true;
    }

    public bool TryResolve() {
        if (!_pending) {
            return false;
        }

        if (Stopwatch.GetTimestamp() - _lastSignal >= _silenceTicks) {
            _pending = false;
            return true;
        }
        return false;
    }
}
