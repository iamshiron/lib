namespace Shiron.Lib.Tess.ThreeMF.Internal;

/// <summary>
/// The built-in writer for standard 3MF documents: writes the document's raw package
/// files verbatim through the shared ZIP writer. It accepts every document type
/// derived from <see cref="ThreeMFDocument"/> that has no more specific writer.
/// </summary>
internal sealed class StandardDocumentWriter : IThreeMFDocumentWriter {
    public Type DocumentType => typeof(ThreeMFDocument);

    public void Write(Stream stream, ThreeMFDocument document, ThreeMFSerializerOptions options) {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(document);

        ThreeMFSerializer.SerializePackage(stream, document.Package, options);
    }
}
