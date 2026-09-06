namespace Shiron.Lib.Tess.ThreeMF.Detection;

/// <summary>
/// Detects and parses one specific kind of 3MF document from a package's raw files.
/// Handlers must be stateless; probing must be cheap and free of side effects.
/// </summary>
internal interface IThreeMFDocumentHandler {
    /// <summary>
    /// Gets the runtime document type this handler produces.
    /// More derived types outrank their base types at equal probe confidence.
    /// </summary>
    Type DocumentType { get; }

    /// <summary>
    /// Estimates how confidently the raw files represent a document of this handler's kind.
    /// Must not throw on malformed input.
    /// </summary>
    /// <param name="context">The immutable probe context.</param>
    /// <returns>The probe result.</returns>
    ProbeResult Probe(ProbeContext context);

    /// <summary>
    /// Parses the raw files into a document of exactly <see cref="DocumentType"/>.
    /// Only invoked on the handler that won document detection.
    /// </summary>
    /// <param name="context">The immutable parse context.</param>
    /// <returns>The parsed document.</returns>
    ThreeMFDocument Parse(ParseContext context);
}
