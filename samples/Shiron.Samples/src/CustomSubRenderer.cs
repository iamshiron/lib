using Shiron.Logging;
using Shiron.Logging.Renderer;
using Shiron.Utils;

namespace Shiron.Samples.Logging;

public class CustomSubRenderer : ILogRenderer {
    public bool RenderLog(ILogEntry entry) {
        switch (entry) {
            case BasicLogEntry e:
                System.Console.WriteLine("Log from CustomSubRenderer:");
                System.Console.WriteLine($"[{TimeUtils.FormatTimestamp(e.Timestamp, false)}{e.LoggerPrefix}] {e.Level}: {e.Message}");
                return true;
            case MarkupLogEntry e:
                System.Console.WriteLine("Log from CustomSubRenderer:");
                System.Console.WriteLine($"[{TimeUtils.FormatTimestamp(e.Timestamp, false)}{e.LoggerPrefix}] {e.Level}: {e.Message} (Markup: {e.Message})");
                return true;
            default:
                return false;
        }
    }
}

