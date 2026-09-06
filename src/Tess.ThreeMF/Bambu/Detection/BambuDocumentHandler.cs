using Shiron.Lib.Tess.ThreeMF.Bambu.Parsing;
using Shiron.Lib.Tess.ThreeMF.Detection;

namespace Shiron.Lib.Tess.ThreeMF.Bambu.Detection;

/// <summary>
/// The built-in handler for Bambu Studio project 3MF documents. It claims packages
/// that declare a 3D model part and carry explicit Bambu producer signatures, such as
/// Bambu/BBL model metadata, <c>X-BBL-*</c> client markers, or the Bambu
/// <c>Metadata/</c> config parts. Its document type derives from the standard one, so
/// it outranks the standard handler at equal confidence.
/// </summary>
internal sealed class BambuDocumentHandler : IThreeMFDocumentHandler {
    public Type DocumentType => typeof(BambuThreeMFDocument);

    public ThreeMFProbeResult Probe(ThreeMFProbeContext context) {
        ArgumentNullException.ThrowIfNull(context);

        return StandardDocumentHandler.DeclaresModelPart(context.Files)
            && BambuSignatures.HasProducerSignature(context.Files)
            ? ThreeMFProbeResult.Certain
            : ThreeMFProbeResult.NoMatch;
    }

    public ThreeMFDocument Parse(ThreeMFParseContext context) => BambuProjectParser.ParseDocument(context);
}
