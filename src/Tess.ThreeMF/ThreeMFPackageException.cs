namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Thrown when a 3MF package cannot be read because it is not a valid ZIP archive
/// or violates the OPC package structural rules.
/// </summary>
public sealed class ThreeMFPackageException : ThreeMFFormatException {
    public ThreeMFPackageException(string message) : base(message) { }

    public ThreeMFPackageException(string message, Exception innerException) : base(message, innerException) { }
}
