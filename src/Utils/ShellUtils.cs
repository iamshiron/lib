using System.Diagnostics;
using System.Text;

namespace Shiron.Lib.Utils;

/// <summary>
/// Provides utility methods for executing shell commands and processes.
/// </summary>
public static class ShellUtils {
    /// <summary>
    /// Represents the result of a process execution.
    /// </summary>
    /// <param name="ExitCode">The exit code of the process.</param>
    /// <param name="StdOut">The standard output produced by the process.</param>
    /// <param name="StdErr">The standard error output produced by the process.</param>
    public record ProcessResult(int ExitCode, string StdOut, string StdErr);

    /// <summary>
    /// Encapsulates all the information required to execute a shell command.
    /// </summary>
    /// <param name="command">The command or executable to run.</param>
    /// <param name="args">The arguments to pass to the command. Defaults to an empty array if null.</param>
    /// <param name="workingDir">The working directory for the command. If null, the current directory is used.</param>
    public sealed class CommandInfo(string command, string[]? args = null, string? workingDir = null) {
        /// <summary>
        /// The command or executable to run.
        /// </summary>
        public string Command = command;
        /// <summary>
        /// The arguments to pass to the command.
        /// </summary>
        public string[] Args = args ?? [];
        /// <summary>
        /// The working directory for the command.
        /// </summary>
        public string? WorkingDir = workingDir;
    }

    /// <summary>
    /// Executes a shell command with an optional working directory.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="args">The command arguments.</param>
    /// <param name="workingDir">Optional working directory where the command will be executed. If null, the current directory is used.</param>
    /// <returns>The process result object containing the exit code, standard output, and standard error.</returns>
    public static ProcessResult Run(string command, string[]? args = null, string? workingDir = null) {
        return Run(new CommandInfo(command, args, workingDir));
    }

    /// <summary>
    /// Executes a shell command with an optional working directory.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="args">The command arguments.</param>
    /// <param name="workingDir">Optional working directory where the command will be executed. If null, the current directory is used.</param>
    /// <returns>The process result object containing the exit code, standard output, and standard error.</returns>
    public static ProcessResult Run(
        string command,
        string[]? args = null,
        string? workingDir = null,
        Action<string>? onStdOut = null,
        Action<string>? onStdErr = null) {
        return Run(new CommandInfo(command, args, workingDir), onStdOut, onStdErr);
    }

    /// <summary>
    /// Executes a shell command using the provided <see cref="CommandInfo"/>.
    /// </summary>
    /// <remarks>
    /// This is the core execution method. It sets up the process, captures standard output and error,
    /// and waits for the command to complete.
    /// Environment variables `TERM` and `FORCE_COLOR` are set to ensure consistent output formatting.
    /// </remarks>
    /// <param name="info">An object containing all configuration for the command execution.</param>
    /// <returns>The process result object containing the exit code, standard output, and standard error.</returns>
    public static ProcessResult Run(CommandInfo info, Action<string>? onStdOut = null, Action<string>? onStdErr = null) {
        var workingDir = info.WorkingDir ?? Directory.GetCurrentDirectory();

        var startInfo = new ProcessStartInfo {
            FileName = info.Command,
            Arguments = string.Join(" ", info.Args),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = workingDir
        };
        startInfo.Environment["TERM"] = "xterm-256color";
        startInfo.Environment["FORCE_COLOR"] = "true";

        using var process = new Process { StartInfo = startInfo };
        StringBuilder stdOutBuilder = new();
        StringBuilder stdErrBuilder = new();

        process.OutputDataReceived += (sender, e) => {
            if (e.Data == null) {
                return;
            }
            _ = stdOutBuilder.AppendLine(e.Data);
            onStdOut?.Invoke(e.Data);
        };

        process.ErrorDataReceived += (sender, e) => {
            if (e.Data == null) {
                return;
            }
            _ = stdErrBuilder.AppendLine(e.Data);
            onStdErr?.Invoke(e.Data);
        };

        _ = process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return new ProcessResult(
            process.ExitCode,
            stdOutBuilder.ToString(),
            stdErrBuilder.ToString()
        );
    }
}
