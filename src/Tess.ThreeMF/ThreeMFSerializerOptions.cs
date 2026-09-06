using System.IO.Compression;

namespace Shiron.Lib.Tess.ThreeMF;

public readonly record struct ThreeMFSerializerOptions() {
    /// <summary>
    /// Gets the compression level applied when writing 3MF packages.
    /// Defaults to <see cref="CompressionLevel.Optimal"/>.
    /// </summary>
    public CompressionLevel CompressionLevel { get; init; } = CompressionLevel.Optimal;

    /// <summary>
    /// Gets the validation strictness applied while reading documents.
    /// Defaults to <see cref="ValidationMode.Standard"/>.
    /// </summary>
    public ValidationMode ValidationMode { get; init; } = ValidationMode.Standard;

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
    public ExtensionCollection Extensions { get; init; } = new();

    public static ThreeMFSerializerOptions Default => new();
}
