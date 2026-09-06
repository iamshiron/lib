namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents a fully read 3MF document: the raw package files, the OPC package view,
/// and the parsed 3MF core models. Serves as the base class for vendor-specific
/// document types, such as Bambu 3MF documents.
/// </summary>
public class ThreeMFDocument {
    /// <summary>
    /// Gets all file parts of the package, keyed by part path.
    /// </summary>
    public required IReadOnlyDictionary<string, ReadOnlyMemory<byte>> Files { get; init; }

    /// <summary>
    /// Gets the OPC package view, including relationships and content types.
    /// </summary>
    public required ThreeMFPackage Package { get; init; }

    /// <summary>
    /// Gets the parsed 3MF core models.
    /// </summary>
    public required ThreeMFCore Core { get; init; }
}
