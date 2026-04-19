using System.Text;
using Shiron.Lib.Logging;
using Shiron.Lib.Logging.Renderer;
using Shiron.Lib.Utils;
using Xunit;

namespace Shiron.Lib.Tests.Logging;

public class LoggerTests {
    private class TestRenderer : ILogRenderer {
        public List<LogPayload<object>> RenderedLogs { get; } = [];

        public bool RenderLog<T>(in LogPayload<T> payload, in ILogger logger) where T : notnull {
            RenderedLogs.Add(new LogPayload<object>(payload.Header, payload.Body));
            return true;
        }
    }

    [Fact]
    public void Logger_BasicLogging_Works() {
        var logger = new Logger(false);
        var renderer = new TestRenderer();
        logger.AddRenderer(renderer);

        logger.Info("Test info message");
        logger.Debug("Test debug message");
        logger.Warning("Test warning message");
        logger.Error("Test error message");
        logger.Critical("Test critical message");
        logger.System("Test system message");

        Assert.Equal(6, renderer.RenderedLogs.Count);
        Assert.Equal(LogLevel.Info, renderer.RenderedLogs[0].Header.Level);
        Assert.Equal("Test info message", ((BasicLogEntry) renderer.RenderedLogs[0].Body).Message);
        Assert.Equal(LogLevel.Debug, renderer.RenderedLogs[1].Header.Level);
        Assert.Equal(LogLevel.Warning, renderer.RenderedLogs[2].Header.Level);
        Assert.Equal(LogLevel.Error, renderer.RenderedLogs[3].Header.Level);
        Assert.Equal(LogLevel.Critical, renderer.RenderedLogs[4].Header.Level);
        Assert.Equal(LogLevel.System, renderer.RenderedLogs[5].Header.Level);
    }

    [Fact]
    public void SubLogger_PrefixInheritance_Works() {
        var logger = new Logger(false);
        var subLogger = logger.CreateSubLogger("Sub");
        var subSubLogger = subLogger.CreateSubLogger("SubSub");

        var renderer = new TestRenderer();
        logger.AddRenderer(renderer);

        subSubLogger.Info("Deep message");

        Assert.Single(renderer.RenderedLogs);
        Assert.Equal("Sub/SubSub", renderer.RenderedLogs[0].Header.Prefix);
    }

    [Fact]
    public void LogInjector_Capture_Works() {
        var logger = new Logger(false);
        // Add a renderer to avoid the unhandled log warning recursion (though now fixed, it's good practice)
        var renderer = new TestRenderer();
        logger.AddRenderer(renderer);

        using var injector = new LogInjector(logger, null, null, false, true).Inject();

        logger.Info("Captured message");

        Assert.Single(injector.CapturedEntries);
        Assert.Equal("Captured message", injector.CapturedEntries[0].Message);
    }

    [Fact]
    public void LogInjector_Suppression_Works() {
        var logger = new Logger(false);
        var renderer = new TestRenderer();
        logger.AddRenderer(renderer);

        using (new LogInjector(logger, null, null, true, false).Inject()) {
            logger.Info("Suppressed message");
        }

        logger.Info("Normal message");

        Assert.Single(renderer.RenderedLogs);
        Assert.Equal("Normal message", ((BasicLogEntry) renderer.RenderedLogs[0].Body).Message);
    }

    [Fact]
    public void LogInjector_Filter_Works() {
        var logger = new Logger(false);
        var renderer = new TestRenderer();
        logger.AddRenderer(renderer);

        using var injector = new LogInjector(logger, null, log => log.Header.Level == LogLevel.Error, false, true).Inject();

        logger.Info("Info message");
        logger.Error("Error message");

        Assert.Single(injector.CapturedEntries);
        Assert.Equal("Error message", injector.CapturedEntries[0].Message);
    }

    [Fact]
    public void LogInjector_Replay_Works() {
        var logger1 = new Logger(false);
        var logger2 = new Logger(false);
        var renderer2 = new TestRenderer();
        logger2.AddRenderer(renderer2);
        logger1.AddRenderer(new TestRenderer());

        using var injector = new LogInjector(logger1, null, null, false, true).Inject();
        logger1.Info("Message 1");
        logger1.Info("Message 2");

        injector.Replay(logger2);

        Assert.Equal(2, renderer2.RenderedLogs.Count);
        Assert.Equal("Message 1", ((CapturedLogEntry) renderer2.RenderedLogs[0].Body).Message);
        Assert.Equal("Message 2", ((CapturedLogEntry) renderer2.RenderedLogs[1].Body).Message);
    }

    [Fact]
    public void JsonLogger_ProducesJsonOutput() {
        using var ms = new MemoryStream();
        var logger = new Logger(true, ms);

        logger.Info("Json message");

        var output = Encoding.UTF8.GetString(ms.ToArray());
        Assert.Contains("\"level\":2", output); // Info is 2
        Assert.Contains("\"body\":{\"message\":\"Json message\"}", output);
    }

    [Fact]
    public void Logger_NoRenderer_DoesNotRecurse() {
        var logger = new Logger(false);
        logger.Info("Unhandled message");
    }
}
