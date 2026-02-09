using System;
using System.IO;
using Shiron.Lib.Logging;
using Shiron.Lib.Logging.Renderer;

namespace Shiron.Samples.Misc;

public class LogRenderer : ILogRenderer {
    private readonly TextWriter _output = Console.Out;

    public bool RenderLog<T>(in LogPayload<T> payload, in ILogger logger) where T : notnull {
        var color = payload.Header.Level switch {
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Info => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.System => ConsoleColor.Cyan,
            _ => ConsoleColor.White,
        };

        Console.ForegroundColor = color;
        _output.Write('[');
        LogRenderUtils.WriteTimestamp(_output, payload.Header.Timestamp);
        _output.Write("] ");

        if (payload.Header.Prefix != null)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            _output.Write('[');
            _output.Write(payload.Header.Prefix);
            _output.Write("] ");
        }

        Console.ForegroundColor = color;
        WriteBody(payload.Body);
        _output.WriteLine();

        return true;
    }

    private void WriteBody<T>(T body) {
        // Use ISpanFormattable for zero-allocation formatting when available
        if (body is ISpanFormattable formattable)
        {
            LogRenderUtils.WriteSpanFormattable(_output, formattable);
        } else
        {
            _output.Write(body?.ToString());
        }
    }
}
