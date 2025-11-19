
using Shiron.Logging;

namespace Shiron.Samples.Logging;

public class LogRenderer {
    public LogRenderer(ILogger logger) {
        logger.OnLogEntry += RenderLogEntry;
    }

    public void RenderLogEntry(ILogEntry entry) {
        switch (entry) {
            case BasicLogEntry e:
                Console.WriteLine($"[{FormatTimestamp(e.Timestamp)}] {e.Level}: {e.Message}");
                break;
            case MarkupLogEntry e:
                Console.WriteLine($"[{FormatTimestamp(e.Timestamp)}] {e.Level}: {e.Message} (Markup: {e.Message})");
                break;
            default:
                Console.WriteLine($"Unknown log entry type: {entry.GetType().Name}");
                break;
        }
    }

    private static string FormatTimestamp(long ms) {
        TimeSpan timeSpan = TimeSpan.FromMilliseconds(ms);
        return timeSpan.ToString(@"hh\:mm\:ss\.fff");
    }
}
