namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// The public extensibility contract for 3MF documents: contributes one document type,
/// a probe that estimates how confidently a package represents it, a parse that
/// produces it, and a writer for it. Extensions add candidates to document detection
/// and serialization; they can never replace or suppress the built-in standard and
/// Bambu handlers and writers. Implementations must be stateless; probing must be
/// cheap and free of side effects.
/// </summary>
public interface IThreeMFExtension {
    /// <summary>
    /// Gets the runtime document type this extension produces. More derived types
    /// outrank their base types at equal probe confidence, and the most specific
    /// registered document type wins writer selection.
    /// </summary>
    Type DocumentType { get; }

    /// <summary>
    /// Estimates how confidently the raw files represent a document of this extension's
    /// kind. Must not throw on malformed input.
    /// </summary>
    /// <param name="context">The immutable probe context.</param>
    /// <returns>The probe result.</returns>
    ThreeMFProbeResult Probe(ThreeMFProbeContext context);

    /// <summary>
    /// Parses the raw files into a document of exactly <see cref="DocumentType"/>.
    /// Only invoked on the extension that won document detection.
    /// </summary>
    /// <param name="context">The immutable parse context.</param>
    /// <returns>The parsed document.</returns>
    ThreeMFDocument Parse(ThreeMFParseContext context);

    /// <summary>
    /// Writes a document of this extension's kind onto the given stream. Only invoked
    /// on the extension that won writer selection. The default implementation writes
    /// the document's raw package files verbatim, mirroring the built-in standard
    /// writer.
    /// </summary>
    /// <param name="stream">The stream to write to; it is left open.</param>
    /// <param name="document">The document to write.</param>
    /// <param name="options">The serialization options in effect.</param>
    void Write(Stream stream, ThreeMFDocument document, ThreeMFSerializerOptions options) {
        ArgumentNullException.ThrowIfNull(document);

        ThreeMFSerializer.SerializePackage(stream, document.Package, options);
    }
}
