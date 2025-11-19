
namespace Shiron.Logging.Renderer;

public interface ILogRenderer {
    /// <summary>Render log entry.</summary>
    /// <param name="entry">Log entry.</param>
    /// <returns>True if rendered. False means the log will be further dispatched down the chain.</returns>
    bool RenderLog(ILogEntry entry);
}
