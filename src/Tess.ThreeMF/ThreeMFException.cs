namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// The root of all exceptions thrown by the 3MF serializer.
/// </summary>
public abstract class ThreeMFException : Exception {
    protected ThreeMFException(string message) : base(message) { }

    protected ThreeMFException(string message, Exception innerException) : base(message, innerException) { }
}
