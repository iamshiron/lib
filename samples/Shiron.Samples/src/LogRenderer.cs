
using Shiron.Logging;
using Shiron.Utils;

namespace Shiron.Samples.Logging;

public class LogRenderer {
    public LogRenderer(ILogger logger) {
        logger.OnLogEntry += RenderLogEntry;
    }

    public void RenderLogEntry(ILogEntry entry) {
        switch (entry) {
            case BasicLogEntry e:
                Console.WriteLine($"[{TimeUtils.FormatTimestamp(e.Timestamp, false)}] {e.Level}: {e.Message}");
                break;
            case MarkupLogEntry e:
                Console.WriteLine($"[{TimeUtils.FormatTimestamp(e.Timestamp, false)}] {e.Level}: {e.Message} (Markup: {e.Message})");
                break;
            default:
                Console.WriteLine($"Unknown log entry type: {entry.GetType().Name}");
                break;
        }
    }
}
