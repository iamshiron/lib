namespace Shiron.Lib.Tess.ThreeMF.Detection;

/// <summary>
/// Adapts a public <see cref="IThreeMFExtension"/> to the internal document handler
/// contract consulted during document detection.
/// </summary>
internal sealed class ExtensionDocumentHandler(IThreeMFExtension extension) : IThreeMFDocumentHandler {
    public Type DocumentType => extension.DocumentType;

    public ProbeResult Probe(ProbeContext context) => extension.Probe(context);

    public ThreeMFDocument Parse(ParseContext context) => extension.Parse(context);
}
