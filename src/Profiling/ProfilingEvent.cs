using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Shiron.Lib.Profiling;

public enum ProfilingEventType : byte {
    BeginEvent = (byte) 'B',
    EndEvent = (byte) 'E',
    CompleteEvent = (byte) 'X',
    ImmediateEvent = (byte) 'I',
    CounterEvent = (byte) 'C'
}

public readonly record struct ProfilingEvent {
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required ProfilingEventType EventType { get; init; }
    public required long Timestamp { get; init; }
    public required int ThreadID { get; init; }
    public required int ProcessID { get; init; }

    public long? Duration { get; init; }
    public Dictionary<string, object>? Args { get; init; }

    public long? AllocationSizeBytes { get; init; }

    // Arbitrary user data
    public string? CounterName { get; init; }
    public long? CounterValue { get; init; }

    public ProfilingEvent() { }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(Utf8JsonWriter writer, JsonSerializerOptions jsonSerializer) {
        writer.WriteStartObject();
        writer.WriteString("name"u8, Name);
        writer.WriteString("cat"u8, Category);
        writer.WriteString("ph"u8, ((char) EventType).ToString());
        writer.WriteNumber("ts"u8, Timestamp);
        writer.WriteNumber("pid"u8, ProcessID);
        writer.WriteNumber("tid"u8, ThreadID);

        writer.WriteStartObject("args"u8);
        if (Args != null)
        {
            foreach (var kvp in Args)
            {
                writer.WritePropertyName(kvp.Key);
                WriteValue(writer, kvp.Value);
            }
        }
        if (AllocationSizeBytes.HasValue)
        {
            writer.WriteNumber("allocationSizeBytes"u8, AllocationSizeBytes.Value);
        }
        if (CounterName != null)
        {
            if (CounterValue.HasValue)
            {
                writer.WriteNumber(CounterName, CounterValue.Value);
            } else
            {
                writer.WriteNull(CounterName);
            }
        }
        writer.WriteEndObject();

        if (Duration.HasValue)
        {
            writer.WriteNumber("dur"u8, Duration.Value);
        }

        writer.WriteEndObject();
    }

    private void WriteValue(Utf8JsonWriter writer, object value) {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string s:
                writer.WriteStringValue(s);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case float f:
                writer.WriteNumberValue(f);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case Dictionary<string, object> dict:
                writer.WriteStartObject();
                foreach (var kvp in dict)
                {
                    writer.WritePropertyName(kvp.Key);
                    WriteValue(writer, kvp.Value);
                }
                writer.WriteEndObject();
                break;
            default:
                writer.WriteStringValue(value.ToString() ?? "null");
                break;
        }
    }
}
