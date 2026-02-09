
using System;

namespace Shiron.Samples.Misc;

public class GCTracker {
    private long _start = 0;

    public void Start() {
        _start = GC.GetAllocatedBytesForCurrentThread();
    }

    public long End() {
        var end = GC.GetAllocatedBytesForCurrentThread();
        return end - _start;
    }
}
