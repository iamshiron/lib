
using Shiron.Lib.Logging;

namespace Shiron.Lib.Utils.Logging;

#region Command Execution Log Entries

/// <summary>Command start.</summary>
public readonly record struct CommandExecutionLogEntry(string Executable, string[] Args, string WorkingDir);

/// <summary>Command finished (success).</summary>
public readonly record struct CommandExecutionFinishedLogEntry(string StdOut, string StdErr, long Duration, int ExitCode);

/// <summary>Command failed.</summary>
public readonly record struct CommandExecutionFailedLogEntry(string StdOut, string StdErr, long Duration, int ExitCode);

/// <summary>Command stdout line.</summary>
public readonly record struct CommandStdOutLogEntry(string Message, bool Quiet);

/// <summary>Command stderr line.</summary>
public readonly record struct CommandStdErrLogEntry(string Message, bool Quiet);

#endregion
