using System.IO.Compression;

namespace Shiron.Lib.Tess.ThreeMF;

public readonly record struct ThreeMFSerializerOptions() {
    /// <summary>
    /// Gets the compression level applied when writing 3MF packages.
    /// </summary>
    public required CompressionLevel CompressionLevel { get; init; }

    /// <summary>
    /// Gets the validation strictness applied while reading documents.
    /// Defaults to <see cref="ValidationMode.Strict"/>.
    /// </summary>
    public ValidationMode ValidationMode { get; init; } = ValidationMode.Strict;

    /// <summary>
    /// Gets a value indicating whether package files that the parsed document does not
    /// recognize are preserved in the resulting <see cref="ThreeMFPackage"/> and
    /// <see cref="ThreeMFDocument"/>. Defaults to <see langword="true"/>.
    /// </summary>
    public bool PreserveUnknownFiles { get; init; } = true;

    /// <summary>
    /// Gets the document handler extensions consulted during document detection.
    /// Registration only adds detection candidates; the built-in standard handler
    /// always participates.
    /// </summary>
    public ThreeMFExtensions Extensions { get; init; } = new();

    public static ThreeMFSerializerOptions Default => new() {
        CompressionLevel = CompressionLevel.Optimal,
    };
}
