namespace Shiron.Lib.Tess.ThreeMF.Exceptions;

/// <summary>
/// Thrown when no registered document handler recognizes a package as a 3MF document,
/// so the runtime document type of the package cannot be determined.
/// </summary>
public class ThreeMFDocumentDetectionException : ThreeMFException {
    public ThreeMFDocumentDetectionException(string message) : base(message) { }

    public ThreeMFDocumentDetectionException(string message, Exception innerException) : base(message, innerException) { }
}
