// See https://aka.ms/new-console-template for more information

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

    // FLow
    typeof(JobSchedulerBenchmarks),

    // Pipeline
    typeof(DagBenchmarks),
    typeof(PipelineBuilderBenchmarks),
    typeof(LinearChainExecutionBenchmarks),
    typeof(WideFanOutExecutionBenchmarks),
    typeof(ComplexGraphExecutionBenchmarks),
    typeof(PipelineSerializationBenchmarks),

    // Utils
    typeof(FunctionUtilsBenchmarks),
    typeof(HashUtilsBenchmarks)
]).Run(args);
