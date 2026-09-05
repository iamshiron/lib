namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents a 3MF row-major 4x3 affine transform: the first three rows form the
/// linear part and the fourth row (<c>M3x</c>) is the translation.
/// </summary>
/// <remarks>
/// Serialized as twelve whitespace-separated numbers in the 3MF
/// <c>transform</c> attribute, e.g. <c>1 0 0 0 1 0 0 0 1 10 20 30</c>.
/// </remarks>
public readonly record struct Transform(
    double M00, double M01, double M02,
    double M10, double M11, double M12,
    double M20, double M21, double M22,
    double M30, double M31, double M32
) {
    /// <summary>
    /// Gets the identity transform (no rotation, no translation).
    /// </summary>
    public static Transform Identity { get; } =
        new(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0);
}
