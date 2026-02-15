using BenchmarkDotNet.Attributes;
using Shiron.Lib.Logging;
using Shiron.Lib.Logging.Renderer;
using Shiron.Lib.Utils;

namespace Shiron.Lib.Benchmarks.Logging;

[MemoryDiagnoser]
public class RendererBenchmarks {
    private Logger _loggerOneRenderer = null!;
    private Logger _loggerFiveRenderers = null!;
    private LogPayload<BasicLogEntry> _payload;
    private StringWriter _writer = null!;

    private class NullRenderer : ILogRenderer {
        public bool RenderLog<T>(in LogPayload<T> payload, in ILogger logger) where T : notnull => true;
    }

    [GlobalSetup]
    public void Setup() {
        _loggerOneRenderer = new Logger(false);
        _loggerOneRenderer.AddRenderer(new NullRenderer());

        _loggerFiveRenderers = new Logger(false);
        for (int i = 0; i < 5; i++) {
            _loggerFiveRenderers.AddRenderer(new NullRenderer());
        }

        _payload = new LogPayload<BasicLogEntry>(
            new LogHeader(LogLevel.Info, "Test", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), null, null),
            new BasicLogEntry("Test message")
        );
        _writer = new StringWriter();
    }

    [Benchmark]
    public void MultipleRenderers_Overhead() {
        _loggerFiveRenderers.Log(_payload);
    }

    [Benchmark]
    public void LogRenderUtils_WriteLogLevel() {
        _writer.GetStringBuilder().Clear();
        LogRenderUtils.WriteLogLevel(_writer, LogLevel.Info);
        LogRenderUtils.WriteLogLevel(_writer, LogLevel.Warning);
        LogRenderUtils.WriteLogLevel(_writer, LogLevel.Error);
    }

    [Benchmark]
    public void LogRenderUtils_WriteTimestamp() {
        _writer.GetStringBuilder().Clear();
        LogRenderUtils.WriteTimestamp(_writer, 1700000000000);
    }

    [Benchmark]
    public void LogRenderUtils_WriteSpanFormattable() {
        _writer.GetStringBuilder().Clear();
        LogRenderUtils.WriteSpanFormattable(_writer, new BasicLogEntry("Test message"));
    }
}
