
using Shiron.Logging;
using Shiron.Logging.Renderer;
using Shiron.Utils;

namespace Shiron.Samples.Logging;

public class LogRenderer : ILogRenderer {
    public bool RenderLog(ILogEntry entry) {
        switch (entry) {
            case BasicLogEntry e:
                Console.WriteLine($"[{TimeUtils.FormatTimestamp(e.Timestamp, false)}/{e.LoggerPrefix}] {e.Level}: {e.Message}");
                return true;
            case MarkupLogEntry e:
                Console.WriteLine($"[{TimeUtils.FormatTimestamp(e.Timestamp, false)}/{e.LoggerPrefix}] {e.Level}: {e.Message} (Markup: {e.Message})");
                return true;
            default:
                return false;
        }
    }
}
