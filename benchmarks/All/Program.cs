using BenchmarkDotNet.Running;
using Shiron.Lib.Collections.Benchmarks;
using Shiron.Lib.Flow.Benchmarks;
using Shiron.Lib.Logging.Benchmarks;
using Shiron.Lib.Utils.Benchmarks;

Console.WriteLine("Hello, World!");

BenchmarkSwitcher.FromTypes([
    // Collections
    typeof(RingBufferBenchmark),

    // Flow
    typeof(LatchedThrottlerBenchmarks),
    typeof(LeadingDebouncerBenchmarks),
    typeof(ThrottlerBenchmarks),
    typeof(TrailingDebouncerBenchmarks),

    // Logging
    typeof(ContextualLoggingBenchmarks),
    typeof(LoggingBenchmarks),
    typeof(RendererBenchmarks),

    // Utils
    typeof(FunctionUtilsBenchmarks),
    typeof(HashUtilsBenchmarks)
]).Run(args);
