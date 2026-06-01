using BenchmarkDotNet.Running;
using Shiron.Lib.Benchmarks.Collections;
using Shiron.Lib.Benchmarks.Concurrency;
using Shiron.Lib.Benchmarks.Flow;
using Shiron.Lib.Benchmarks.Logging;
using Shiron.Lib.Benchmarks.Pipeline;
using Shiron.Lib.Benchmarks.Utils;

Console.WriteLine("Hello, World!");

BenchmarkSwitcher.FromTypes([
    // Collections
    typeof(RingBufferBenchmark),
    typeof(LatchedThrottlerBenchmarks),
    typeof(LeadingDebouncerBenchmarks),
    typeof(ThrottlerBenchmarks),
    typeof(TrailingDebouncerBenchmarks),

    // Concurrency
    typeof(ContextualLoggingBenchmarks),
    typeof(LoggingBenchmarks),
    typeof(RendererBenchmarks),

    // Flow
    typeof(JobSchedulerBenchmarks),

    // Pipeline
    typeof(PipelineExecutionBenchmarks),
    typeof(PipelineBuilderBenchmarks),
    typeof(PipelineContextBenchmarks),

    // Utils
    typeof(FunctionUtilsBenchmarks),
    typeof(HashUtilsBenchmarks)
]).Run(args);
