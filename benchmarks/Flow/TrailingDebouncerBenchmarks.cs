using BenchmarkDotNet.Attributes;
using Shiron.Lib.Flow;

namespace Shiron.Lib.Benchmarks.Flow;

[MemoryDiagnoser]
public class TrailingDebouncerBenchmarks {
    private TrailingDebouncer _debouncer = null!;

    [Params(10, 100, 1000)]
    public long SilenceTimeMS { get; set; }

    [GlobalSetup]
    public void Setup() {
        _debouncer = new TrailingDebouncer(SilenceTimeMS);
    }

    [Benchmark]
    public void Signal() {
        _debouncer.Signal();
    }

    [Benchmark]
    public bool TryResolve_NoPending() {
        // Resolve any pending state first
        while (_debouncer.TryResolve()) { }
        return _debouncer.TryResolve();
    }

    [Benchmark]
    public bool TryResolve_WithPending_NotElapsed() {
        _debouncer.Signal();
        return _debouncer.TryResolve();
    }

    /// <summary>
    /// Benchmarks the typical trailing debounce pattern:
    /// Multiple signals followed by repeated resolve checks
    /// </summary>
    [Benchmark]
    public int TrailingPattern_10Signals_100Resolves() {
        int successCount = 0;

        // Simulate burst of signals (user typing/input)
        for (int i = 0; i < 10; i++) {
            _debouncer.Signal();
        }

        // Poll for resolution (typical in event loops)
        for (int i = 0; i < 100; i++) {
            if (_debouncer.TryResolve()) {
                successCount++;
            }
        }

        return successCount;
    }

    /// <summary>
    /// Benchmarks signal overhead in rapid succession
    /// </summary>
    [Benchmark]
    public void RapidSignaling_100Times() {
        for (int i = 0; i < 100; i++) {
            _debouncer.Signal();
        }
    }

    /// <summary>
    /// Benchmarks polling overhead when nothing is pending
    /// </summary>
    [Benchmark]
    public int EmptyPolling_100Times() {
        // Clear any pending state
        while (_debouncer.TryResolve()) { }

        int successCount = 0;
        for (int i = 0; i < 100; i++) {
            if (_debouncer.TryResolve()) {
                successCount++;
            }
        }

        return successCount;
    }

    /// <summary>
    /// Benchmarks alternating signal and immediate resolve pattern
    /// (signal should be continually reset, never resolving)
    /// </summary>
    [Benchmark]
    public int AlternatingSignalResolve_50Cycles() {
        int successCount = 0;

        for (int i = 0; i < 50; i++) {
            _debouncer.Signal();
            if (_debouncer.TryResolve()) {
                successCount++;
            }
        }

        return successCount;
    }
}
