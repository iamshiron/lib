
namespace Shiron.Lib.Profiling;

/// <summary>Utility functions for profiling.</summary>
public static class Utils {
    /// <summary>Gets the current time in milliseconds since the Unix epoch.</summary>
    public static long TimeNow() {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
