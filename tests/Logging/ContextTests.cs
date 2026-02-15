using Shiron.Lib.Logging;
using Shiron.Lib.Logging.Renderer;
using Shiron.Lib.Utils;
using Xunit;

namespace Shiron.Lib.Tests.Logging;

public class ContextTests {
    private class TestRenderer : ILogRenderer {
        public List<LogPayload<object>> RenderedLogs { get; } = [];

        public bool RenderLog<T>(in LogPayload<T> payload, in ILogger logger) where T : notnull {
            RenderedLogs.Add(new LogPayload<object>(payload.Header, payload.Body));
            return true;
        }
    }

    [Fact]
    public void ContextualLogger_InheritsContextIDs() {
        var logger = new Logger(false);
        var renderer = new TestRenderer();
        logger.AddRenderer(renderer);

        logger.Info("Root", out var context1);
        context1.Info("Child", out var context2);
        context2.Info("Grandchild");

        Assert.Equal(3, renderer.RenderedLogs.Count);

        // Root log: logger.Info("Root", out var context1);
        // This creates context1 with a new ID, and logs using that ID as ContextID.
        Assert.Equal(context1.ContextID, renderer.RenderedLogs[0].Header.ContextID);
        Assert.Null(renderer.RenderedLogs[0].Header.ParentContextID);

        // Child log: context1.Info("Child", out var context2);
        // This creates context2 with a new ID, and logs using context2.ContextID as ContextID 
        // and context1.ContextID as ParentContextID.
        Assert.Equal(context2.ContextID, renderer.RenderedLogs[1].Header.ContextID);
        Assert.Equal(context1.ContextID, renderer.RenderedLogs[1].Header.ParentContextID);

        // Grandchild log: context2.Info("Grandchild");
        // This logs with ContextID = null (as it doesn't create a new context)
        // and ParentContextID = context2.ContextID.
        Assert.Null(renderer.RenderedLogs[2].Header.ContextID);
        Assert.Equal(context2.ContextID, renderer.RenderedLogs[2].Header.ParentContextID);
    }
}
