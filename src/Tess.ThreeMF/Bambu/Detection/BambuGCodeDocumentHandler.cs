using Shiron.Lib.Tess.ThreeMF.Bambu.Parsing;
using Shiron.Lib.Tess.ThreeMF.Detection;

namespace Shiron.Lib.Tess.ThreeMF.Bambu.Detection;

/// <summary>
/// The built-in handler for sliced Bambu G-code 3MF documents. It claims packages the
/// Bambu handler claims that additionally embed a sliced plate G-code part such as
/// <c>Metadata/plate_1.gcode</c>. Its document type derives from the Bambu project
/// one, so it outranks the Bambu handler at equal confidence.
/// </summary>
internal sealed class BambuGCodeDocumentHandler : IThreeMFDocumentHandler {
    public Type DocumentType => typeof(BambuGCodeDocument);

    public ProbeResult Probe(ProbeContext context) {
        ArgumentNullException.ThrowIfNull(context);

        return StandardDocumentHandler.DeclaresModelPart(context.Files)
            && BambuSignatures.HasProducerSignature(context.Files)
            && BambuSignatures.HasSlicedGCode(context.Files)
            ? ProbeResult.Certain
            : ProbeResult.NoMatch;
    }

    public ThreeMFDocument Parse(ParseContext context) => BambuProjectParser.ParseGCodeDocument(context);
}
