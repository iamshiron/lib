namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// The immutable input shared by all document probes: the raw package files keyed by
/// part path, plus the serializer options in effect. Probing must be cheap and free of
/// side effects.
/// </summary>
public sealed class ThreeMFProbeContext {
    /// <summary>
    /// Gets the immutable raw package files, keyed by part path.
    /// </summary>
    public required IReadOnlyDictionary<string, ReadOnlyMemory<byte>> Files { get; init; }

    /// <summary>
    /// Gets the serializer options in effect.
    /// </summary>
    public required ThreeMFSerializerOptions Options { get; init; }
}
