namespace Shiron.Lib.Tess.ThreeMF.Internal;

/// <summary>
/// The built-in writer for Bambu Studio project 3MF documents: preserves the
/// document's raw package files, including the Bambu <c>Metadata/</c> config parts,
/// verbatim through the shared ZIP writer.
/// </summary>
internal sealed class BambuDocumentWriter : IThreeMFDocumentWriter {
    public Type DocumentType => typeof(BambuThreeMFDocument);

    public void Write(Stream stream, ThreeMFDocument document, ThreeMFSerializerOptions options) {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(document);

        ThreeMFSerializer.SerializePackage(stream, document.Package, options);
    }
}
