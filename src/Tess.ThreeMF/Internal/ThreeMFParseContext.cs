namespace Shiron.Lib.Tess.ThreeMF.Internal;

/// <summary>
/// The immutable input shared by all document handlers during probing and parsing:
/// the raw package files keyed by part path, plus the serializer options in effect.
/// </summary>
internal sealed class ThreeMFParseContext {
    /// <summary>
    /// Gets the immutable raw package files, keyed by part path.
    /// </summary>
    public required IReadOnlyDictionary<string, ReadOnlyMemory<byte>> Files { get; init; }

    /// <summary>
    /// Gets the serializer options in effect.
    /// </summary>
    public required ThreeMFSerializerOptions Options { get; init; }
}
