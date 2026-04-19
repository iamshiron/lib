using BenchmarkDotNet.Attributes;
using Shiron.Lib.Logging;
using Shiron.Lib.Logging.Renderer;
using Shiron.Lib.Utils;

namespace Shiron.Lib.Benchmarks.Logging;

[MemoryDiagnoser]
public class ContextualLoggingBenchmarks {
    private Logger _logger = null!;

    private class NullRenderer : ILogRenderer {
        public bool RenderLog<T>(in LogPayload<T> payload, in ILogger logger) where T : notnull => true;
    }

    [GlobalSetup]
    public void Setup() {
        _logger = new Logger(false);
        _logger.AddRenderer(new NullRenderer());
    }

    [Benchmark]
    public void RootContextCreation() {
        _logger.Info("Root", out _);
    }

    [Benchmark]
    public void ChildContextCreation() {
        _logger.Info("Root", out var context);
        context.Info("Child", out _);
    }

    [Benchmark]
    public void DeepContextualLogging() {
        _logger.Info("1", out var c1);
        c1.Info("2", out var c2);
        c2.Info("3", out var c3);
        c3.Info("4");
    }
}
