using System.Buffers;

namespace Shiron.Lib.Logging;

public readonly record struct LogHeader(
    LogLevel Level,
    string? Prefix,
    long Timestamp,
    Guid? ContextId
);

public readonly record struct LogPayload<T>(
    LogHeader Header,
    T Body
);

/// <summary>Message entry.</summary>
public readonly record struct BasicLogEntry(string Message) : ISpanFormattable {
    /// <summary>Format to span without allocation.</summary>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
        if (Message == null)
        {
            charsWritten = 0;
            return true;
        }
        if (Message.Length <= destination.Length)
        {
            Message.AsSpan().CopyTo(destination);
            charsWritten = Message.Length;
            return true;
        }
        charsWritten = 0;
        return false;
    }

    /// <summary>Fallback for string formatting.</summary>
    public string ToString(string? format, IFormatProvider? formatProvider) => Message ?? string.Empty;
}

/// <summary>Markup (Info level) entry.</summary>
public readonly record struct MarkupLogEntry(string Message) : ISpanFormattable {
    /// <summary>Format to span without allocation.</summary>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
        if (Message == null)
        {
            charsWritten = 0;
            return true;
        }
        if (Message.Length <= destination.Length)
        {
            Message.AsSpan().CopyTo(destination);
            charsWritten = Message.Length;
            return true;
        }
        charsWritten = 0;
        return false;
    }

    /// <summary>Fallback for string formatting.</summary>
    public string ToString(string? format, IFormatProvider? formatProvider) => Message ?? string.Empty;
}

/// <summary>A log entry captured by an injector <see cref="LogInjector"/>.</summary>
public readonly record struct CapturedLogEntry : ISpanFormattable {
    // Store the string directly if it's a basic entry
    public string? Message { get; }

    // Only use this for complex custom structs
    public object? RawData { get; }

    public CapturedLogEntry(string message) {
        Message = message;
        RawData = null;
    }

    public CapturedLogEntry(object data) {
        Message = null;
        RawData = data;
    }

    /// <summary>Format to span without allocation.</summary>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
        var content = Message ?? RawData?.ToString() ?? string.Empty;
        if (content.Length <= destination.Length)
        {
            content.AsSpan().CopyTo(destination);
            charsWritten = content.Length;
            return true;
        }
        charsWritten = 0;
        return false;
    }

    /// <summary>Fallback for string formatting.</summary>
    public string ToString(string? format, IFormatProvider? formatProvider) => Message ?? RawData?.ToString() ?? string.Empty;
}
