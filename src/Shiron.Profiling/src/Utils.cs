
namespace Shiron.Profiling;

public static class Utils {
    public static long TimeNow() {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
