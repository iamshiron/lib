namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Thrown when producer-specific Bambu metadata of a 3MF package is present but
/// structurally malformed and cannot be read.
/// </summary>
public sealed class BambuFormatException : ThreeMFFormatException {
    public BambuFormatException(string message) : base(message) { }

    public BambuFormatException(string message, Exception innerException) : base(message, innerException) { }
}
