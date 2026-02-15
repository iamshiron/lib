using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Shiron.Lib.Logging;
using Shiron.Lib.Logging.Renderer;
using Shiron.Lib.Utils;

namespace Shiron.Lib.Benchmarks.Logging;

[MemoryDiagnoser]
public class LoggingBenchmarks {
    private Logger _logger = null!;
    private Logger _jsonLogger = null!;
    private Logger _noRendererLogger = null!;
    private Stream _nullStream = Stream.Null;
    private LogInjector _captureInjector = null!;
    private LogInjector _suppressInjector = null!;

    private class NullRenderer : ILogRenderer {
        public bool RenderLog<T>(in LogPayload<T> payload, in ILogger logger) where T : notnull {
            return true;
        }
    }

    [GlobalSetup]
    public void Setup() {
        _logger = new Logger(false);
        _logger.AddRenderer(new NullRenderer());

        _jsonLogger = new Logger(true, _nullStream);

        _noRendererLogger = new Logger(false);

        _captureInjector = new LogInjector(_logger, null, null, false, true);
        _suppressInjector = new LogInjector(_logger, null, null, true, false);
    }

    [Benchmark]
    public void BasicLogging_NoRenderer() {
        _noRendererLogger.Info("Test message");
    }

    [Benchmark]
    public void BasicLogging_WithRenderer() {
        _logger.Info("Test message");
    }

    [Benchmark]
    public void JsonLogging() {
        _jsonLogger.Info("Test message");
    }

    [Benchmark]
    public void Logging_WithCaptureInjector() {
        using var inj = new LogInjector(_logger, null, null, false, true).Inject();
        _logger.Info("Test message");
    }

    [Benchmark]
    public void Logging_WithSuppressInjector() {
        using var inj = LogInjector.CreateSuppressor(_logger).Inject();
        _logger.Info("Test message");
    }
}
