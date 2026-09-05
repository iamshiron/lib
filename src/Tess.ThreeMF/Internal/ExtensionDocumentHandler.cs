namespace Shiron.Lib.Tess.ThreeMF.Internal;

/// <summary>
/// Adapts a public <see cref="IThreeMFExtension"/> to the internal document handler
/// contract consulted during document detection.
/// </summary>
internal sealed class ExtensionDocumentHandler(IThreeMFExtension extension) : IThreeMFDocumentHandler {
    public Type DocumentType => extension.DocumentType;

    public ThreeMFProbeResult Probe(ThreeMFProbeContext context) => extension.Probe(context);

    public ThreeMFDocument Parse(ThreeMFParseContext context) => extension.Parse(context);
}
