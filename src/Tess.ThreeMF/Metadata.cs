namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents a <c>&lt;metadata&gt;</c> entry of a 3MF model, e.g.
/// <c>&lt;metadata name="Title"&gt;My Part&lt;/metadata&gt;</c>.
/// </summary>
public sealed record Metadata {
    /// <summary>
    /// Gets the metadata name, e.g. <c>Title</c>.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the metadata value text.
    /// </summary>
    public required string Value { get; init; }
}
