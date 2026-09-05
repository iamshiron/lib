namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents a <c>&lt;component&gt;</c> of a 3MF object: a reference to another object
/// resource with an optional transform applied to it.
/// </summary>
public sealed record Component {
    /// <summary>
    /// Gets the resource id of the referenced object.
    /// </summary>
    public required int ObjectId { get; init; }

    /// <summary>
    /// Gets the transform applied to the referenced object,
    /// or <see langword="null"/> when the component omits the attribute.
    /// </summary>
    public Transform? Transform { get; init; }
}
