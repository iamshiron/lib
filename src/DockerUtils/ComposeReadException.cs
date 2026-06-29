namespace Shiron.Lib.DockerUtils;

/// <summary>
/// Thrown when a Docker Compose file cannot be read or contains an unsupported shape.
/// </summary>
public sealed class ComposeReadException : Exception {
    public ComposeReadException(string message) : base(message) { }

    public ComposeReadException(string message, Exception innerException) : base(message, innerException) { }
}
