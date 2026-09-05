namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents the thumbnail images of a Bambu print plate.
/// </summary>
public sealed class BambuPlateThumbnail {
    /// <summary>
    /// Gets the part path of the plate image, e.g. <c>Metadata/plate_1.png</c>. When
    /// only a small image exists, this is the part path of the small image.
    /// </summary>
    public required string PartPath { get; init; }

    /// <summary>
    /// Gets the part path of the small plate image, e.g.
    /// <c>Metadata/plate_1_small.png</c>, or <see langword="null"/> when absent.
    /// </summary>
    public string? SmallPartPath { get; init; }

    /// <summary>
    /// Gets the raw image bytes of <see cref="PartPath"/>.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; init; }

    /// <summary>
    /// Gets the raw image bytes of <see cref="SmallPartPath"/>, or empty when absent.
    /// </summary>
    public ReadOnlyMemory<byte> SmallData { get; init; }
}
