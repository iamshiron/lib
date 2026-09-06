using Shiron.Lib.Tess.ThreeMF.Writing;

namespace Shiron.Lib.Tess.ThreeMF.Bambu.Writing;

/// <summary>
/// The built-in writer for sliced Bambu G-code 3MF documents: preserves the
/// document's raw package files, including the embedded sliced plate G-code parts,
/// verbatim through the shared ZIP writer.
/// </summary>
internal sealed class BambuGCodeDocumentWriter : IThreeMFDocumentWriter {
    public Type DocumentType => typeof(BambuGCodeDocument);

    public void Write(Stream stream, ThreeMFDocument document, ThreeMFSerializerOptions options) {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(document);

        ThreeMFSerializer.SerializePackage(stream, document.Package, options);
    }
}
