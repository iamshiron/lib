
namespace Shiron.Lib.Utils;

/// <summary>
/// Utility functions for time-related operations.
/// </summary>
public static class TimeUtils {
    /// <summary>
    /// Gets the current time in milliseconds since the Unix epoch (January 1, 1970).
    /// </summary>
    /// <returns>The current time in milliseconds.</returns>
    public static long Now() {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Formats a timestamp in milliseconds into a human-readable string (hh:mm:ss.fff).
    /// </summary>
    /// <param name="ms">The timestamp in milliseconds.</param>
    /// <param name="includeMS">Whether to include milliseconds in the output.</param>
    /// <returns>A formatted time string.</returns>
    public static string FormatTimestamp(long ms, bool includeMS = true) {
        TimeSpan timeSpan = TimeSpan.FromMilliseconds(ms);
        return includeMS ? timeSpan.ToString(@"hh\:mm\:ss\.fff") : timeSpan.ToString(@"hh\:mm\:ss");
    }
}
