namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents a build <c>&lt;item&gt;</c> of a 3MF model: a reference to an object
/// resource with an optional transform applied to it.
/// </summary>
public sealed record BuildItem {
    /// <summary>
    /// Gets the resource id of the object to build.
    /// </summary>
    public required int ObjectId { get; init; }

    /// <summary>
    /// Gets the transform applied to the built object,
    /// or <see langword="null"/> when the item omits the attribute.
    /// </summary>
    public Transform? Transform { get; init; }

    /// <summary>
    /// Gets the part number of the item,
    /// or <see langword="null"/> when the item omits the attribute.
    /// </summary>
    public string? PartNumber { get; init; }
}
