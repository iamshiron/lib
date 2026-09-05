namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents one object placed on a Bambu print plate, referencing an object
/// resource of the 3MF core model.
/// </summary>
public sealed record BambuPlateObject {
    /// <summary>
    /// Gets the id of the referenced object resource in the 3MF core model.
    /// </summary>
    public required int ObjectId { get; init; }

    /// <summary>
    /// Gets the object name, or <see langword="null"/> when the plate declaration
    /// carries only the object id.
    /// </summary>
    public string? Name { get; init; }
}
