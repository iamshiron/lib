namespace Shiron.Lib.Tess.ThreeMF.Detection;

/// <summary>
/// The immutable input passed to the winning document parse: the raw package files
/// keyed by part path, plus the serializer options in effect.
/// </summary>
public sealed class ParseContext {
    /// <summary>
    /// Gets the immutable raw package files, keyed by part path.
    /// </summary>
    public required IReadOnlyDictionary<string, ReadOnlyMemory<byte>> Files { get; init; }

    /// <summary>
    /// Gets the serializer options in effect.
    /// </summary>
    public required ThreeMFSerializerOptions Options { get; init; }
}
