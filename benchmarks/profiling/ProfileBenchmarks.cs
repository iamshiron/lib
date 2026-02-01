using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Shiron.Lib.Logging;
using Shiron.Lib.Profiling;

namespace Shiron.Docs.Engine.Benchmarks;

[MemoryDiagnoser] // <--- REQUIRED for memory tracking
[SimpleJob(RunStrategy.Throughput)] // measures raw speed
public class ProfilerBenchmarks {
    private Profiler _profiler;

    [IterationSetup]
    public void Setup() {
        _profiler = new Profiler(null);
    }

    [Benchmark]
    public void Benchmark_BeginEvent() {
        _profiler.BeginEvent("BenchmarkTest");
    }

    [Benchmark]
    public void Benchmark_Scope_Using() {
        using (new ProfileScope(_profiler, "ScopeTest")) {
        }
    }

    [Benchmark]
    public void Benchmark_Counter() {
        _profiler.RecordCounter("Counter", "Value", 12345);
    }
}
