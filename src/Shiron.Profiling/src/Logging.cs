
using System.Text.Json.Serialization;
using Shiron.Logging;
using Shiron.Profiling;

namespace Shiron.Profiling;

public readonly record struct ProfilingLogEntry(string Name, string Category, long ProcessID, long ThreadID, long TraceTimestamp);
public readonly record struct ProfileBeingLogEntry(string Name, string Category, long ProcessID, long ThreadID, long TraceTimestamp);
public readonly record struct ProfileCompleteLogEntry(string Name, string Category, long ProcessID, long ThreadID, long TraceTimestamp, long Duration);
