using System.Text.Json;
using System.Text.Json.Serialization;
using Shiron.Lib.Logging;
using Shiron.Lib.Logging.Renderer;

namespace Shiron.Lib.Logging.Renderer;

public class JsonLogRenderer : ILogRenderer, IDisposable {
    private readonly Stream _outputStream;
    private readonly Lock _writeLock = new();
    private readonly Lock _optionsLock = new();

    private readonly Utf8JsonWriter _writer;

    // Cache the writer options to avoid allocation
    private readonly JsonWriterOptions _writerOptions = new() {
        Indented = false,
        SkipValidation = true
    };

    private JsonSerializerOptions _serializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public JsonLogRenderer(Stream outputStream) {
        _outputStream = outputStream;
        _writer = new Utf8JsonWriter(_outputStream, _writerOptions);
    }

    public void RegisterConverters(IEnumerable<JsonConverter> converters) {
        lock (_optionsLock)
        {
            var copy = new JsonSerializerOptions(_serializerOptions);
            foreach (var converter in converters)
            {
                copy.Converters.Add(converter);
            }
            _serializerOptions = copy;
        }
    }

    public bool RenderLog<T>(in LogPayload<T> payload, in ILogger logger) where T : notnull {
        lock (_writeLock)
        {
            _writer.Reset(_outputStream);

            _writer.WriteStartObject();

            _writer.WriteNumber("timestamp"u8, payload.Header.Timestamp);
            _writer.WriteNumber("level"u8, (int) payload.Header.Level);
            _writer.WriteString("contextId"u8, payload.Header.ContextId?.ToString());
            _writer.WriteString("type"u8, typeof(T).FullName ?? "unknown");
            if (payload.Header.Prefix != null) _writer.WriteString("prefix"u8, payload.Header.Prefix);

            _writer.WritePropertyName("body"u8);
            JsonSerializer.Serialize(_writer, payload.Body, _serializerOptions);

            _writer.WriteEndObject();

            _writer.Flush();
            _outputStream.WriteByte((byte) '\n');
        }
        return true;
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
        _writer.Dispose();
        _outputStream.Dispose();
    }
}
