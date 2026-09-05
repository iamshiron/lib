namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Thrown when the 3D model part of a 3MF package cannot be read because it is
/// malformed XML or violates the 3MF core specification validation rules, such as
/// references to unknown objects or mesh triangle indices pointing outside the
/// vertex list.
/// </summary>
public sealed class ThreeMFCoreException : ThreeMFFormatException {
    public ThreeMFCoreException(string message) : base(message) { }

    public ThreeMFCoreException(string message, Exception innerException) : base(message, innerException) { }
}
