using BenchmarkDotNet.Attributes;
using Shiron.Lib.Flow;

namespace Shiron.Lib.Benchmarks.Flow;

[MemoryDiagnoser]
public class ThrottlerBenchmarks {
    private Throttler _throttler = null!;

    [Params(10, 100, 1000)]
    public long IntervalMS { get; set; }

    [GlobalSetup]
    public void Setup() {
        _throttler = new Throttler(IntervalMS);
    }

    [Benchmark]
    public bool TryExecute_WhenAllowed() {
        // Reset to ensure we're always in a state where execution is allowed
        _throttler.Reset();
        return _throttler.TryExecute();
    }

    [Benchmark]
    public bool TryExecute_WhenThrottled() {
        // First call succeeds, second is throttled
        _throttler.Reset();
        _throttler.TryExecute();
        return _throttler.TryExecute();
    }

    [Benchmark]
    public float CooldownProgress() {
        return _throttler.CooldownProgress();
    }

    [Benchmark]
    public float GetTimeRemainingMS() {
        return _throttler.GetTimeRemainingMS();
    }

    [Benchmark]
    public TimeSpan GetTimeRemaining() {
        return _throttler.GetTimeRemaining();
    }

    [Benchmark]
    public void Reset() {
        _throttler.Reset();
    }

    /// <summary>
    /// Benchmarks a realistic burst scenario: 100 rapid calls where most should be throttled
    /// </summary>
    [Benchmark]
    public int BurstScenario_100Calls() {
        _throttler.Reset();
        int successCount = 0;

        for (int i = 0; i < 100; i++) {
            if (_throttler.TryExecute()) {
                successCount++;
            }
        }

        return successCount;
    }
}
