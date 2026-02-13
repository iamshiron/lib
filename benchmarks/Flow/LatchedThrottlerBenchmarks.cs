using BenchmarkDotNet.Attributes;
using Shiron.Lib.Flow;

namespace Shiron.Lib.Benchmarks.Flow;

[MemoryDiagnoser]
public class LatchedThrottlerBenchmarks {
    private LatchedThrottler _throttler = null!;

    [Params(10, 100, 1000)]
    public long IntervalMS { get; set; }

    [GlobalSetup]
    public void Setup() {
        _throttler = new LatchedThrottler(IntervalMS);
    }

    [Benchmark]
    public void Trigger() {
        _throttler.Trigger();
    }

    [Benchmark]
    public bool Update_WithoutTrigger() {
        _throttler.Clear();
        return _throttler.Update();
    }

    [Benchmark]
    public bool Update_WithTrigger_NotElapsed() {
        _throttler.Reset();
        _throttler.Trigger();
        return _throttler.Update();
    }

    [Benchmark]
    public void Clear() {
        _throttler.Clear();
    }

    [Benchmark]
    public void Reset() {
        _throttler.Reset();
    }

    /// <summary>
    /// Benchmarks a realistic scenario: trigger once, then poll with Update() 100 times
    /// </summary>
    [Benchmark]
    public int TriggerAndPoll_100Updates() {
        _throttler.Reset();
        _throttler.Trigger();
        int successCount = 0;

        for (int i = 0; i < 100; i++) {
            if (_throttler.Update()) {
                successCount++;
            }
        }

        return successCount;
    }

    /// <summary>
    /// Benchmarks repeated triggering without updates (latch overwrite scenario)
    /// </summary>
    [Benchmark]
    public void RepeatedTriggers_100Times() {
        _throttler.Reset();

        for (int i = 0; i < 100; i++) {
            _throttler.Trigger();
        }
    }
}
