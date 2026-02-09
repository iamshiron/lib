namespace Shiron.Lib.Logging.Renderer;

public interface ILogRenderer {
    /// <summary>Render log entry.</summary>
    /// <typeparam name="T">Log entry data type.</typeparam>
    /// <param name="payload">Log payload.</param>
    /// <param name="logger">Logger instance for logging back on custom entries.</param>
    /// <returns>Whether the log was rendered.</returns>
    bool RenderLog<T>(in LogPayload<T> payload, in ILogger logger) where T : notnull;
}

/// <summary>Helper utilities for low-allocation log rendering.</summary>
public static class LogRenderUtils {
    // Shared buffer for formatting - use ThreadStatic to avoid contention
    [ThreadStatic]
    private static char[]? _formatBuffer;

    /// <summary>Gets a thread-local format buffer (512 chars).</summary>
    public static Span<char> GetFormatBuffer() {
        _formatBuffer ??= new char[512];
        return _formatBuffer.AsSpan();
    }

    /// <summary>Write timestamp to TextWriter without allocation (HH:mm:ss format).</summary>
    public static void WriteTimestamp(TextWriter writer, long unixTimeMs) {
        var dto = DateTimeOffset.FromUnixTimeMilliseconds(unixTimeMs);
        Span<char> buffer = stackalloc char[8]; // HH:mm:ss

        var hour = dto.Hour;
        var minute = dto.Minute;
        var second = dto.Second;

        buffer[0] = (char) ('0' + hour / 10);
        buffer[1] = (char) ('0' + hour % 10);
        buffer[2] = ':';
        buffer[3] = (char) ('0' + minute / 10);
        buffer[4] = (char) ('0' + minute % 10);
        buffer[5] = ':';
        buffer[6] = (char) ('0' + second / 10);
        buffer[7] = (char) ('0' + second % 10);

        writer.Write(buffer);
    }

    /// <summary>Write a span-formattable value without allocation.</summary>
    public static void WriteSpanFormattable<T>(TextWriter writer, T value) where T : ISpanFormattable {
        Span<char> buffer = GetFormatBuffer();
        if (value.TryFormat(buffer, out int charsWritten, default, null))
        {
            writer.Write(buffer[..charsWritten]);
        } else
        {
            // Fallback to string if buffer too small
            writer.Write(value.ToString());
        }
    }

    /// <summary>Write log level name without allocation.</summary>
    public static void WriteLogLevel(TextWriter writer, LogLevel level) {
        ReadOnlySpan<char> name = level switch {
            LogLevel.System => "SYS",
            LogLevel.Debug => "DBG",
            LogLevel.Info => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            _ => "???"
        };
        writer.Write(name);
    }
}
