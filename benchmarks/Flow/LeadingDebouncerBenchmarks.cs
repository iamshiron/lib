using BenchmarkDotNet.Attributes;
using Shiron.Lib.Flow;

namespace Shiron.Lib.Benchmarks.Flow;

[MemoryDiagnoser]
public class LeadingDebouncerBenchmarks {
    private LeadingDebouncer _debouncer = null!;

    [Params(10, 100, 1000)]
    public long SilenceTimeMS { get; set; }

    [GlobalSetup]
    public void Setup() {
        _debouncer = new LeadingDebouncer(SilenceTimeMS);
    }

    [Benchmark]
    public bool TryExecute_FirstCall() {
        // Reset ensures first call will succeed
        _debouncer.Reset();
        return _debouncer.TryExecute();
    }

    [Benchmark]
    public bool TryExecute_ImmediateSecondCall() {
        // Reset and first call, then immediate second call (should fail)
        _debouncer.Reset();
        _debouncer.TryExecute();
        return _debouncer.TryExecute();
    }

    [Benchmark]
    public void Reset() {
        _debouncer.Reset();
    }

    /// <summary>
    /// Benchmarks rapid-fire debouncing: 100 calls in quick succession
    /// Only the first should succeed, rest should be debounced
    /// </summary>
    [Benchmark]
    public int RapidFire_100Calls() {
        _debouncer.Reset();
        int successCount = 0;

        for (int i = 0; i < 100; i++) {
            if (_debouncer.TryExecute()) {
                successCount++;
            }
        }

        return successCount;
    }

    /// <summary>
    /// Benchmarks the overhead of the interlocked operation in TryExecute
    /// </summary>
    [Benchmark]
    public bool TryExecute_InterlockedOverhead() {
        return _debouncer.TryExecute();
    }

    /// <summary>
    /// Benchmarks alternating pattern: call, wait (via reset), call, wait
    /// Tests the best-case scenario where debouncer allows execution
    /// </summary>
    [Benchmark]
    public int AlternatingPattern_50Cycles() {
        int successCount = 0;

        for (int i = 0; i < 50; i++) {
            _debouncer.Reset();
            if (_debouncer.TryExecute()) {
                successCount++;
            }
        }

        return successCount;
    }
}
