namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// The base of all exceptions thrown when 3MF or OPC data is malformed and cannot be
/// read, regardless of the specific part that violates its format.
/// </summary>
public abstract class ThreeMFFormatException : ThreeMFException {
    protected ThreeMFFormatException(string message) : base(message) { }

    protected ThreeMFFormatException(string message, Exception innerException) : base(message, innerException) { }
}
