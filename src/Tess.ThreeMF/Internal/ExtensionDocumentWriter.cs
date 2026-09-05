namespace Shiron.Lib.Tess.ThreeMF.Internal;

/// <summary>
/// Adapts a public <see cref="IThreeMFExtension"/> to the internal document writer
/// contract consulted during writer selection.
/// </summary>
internal sealed class ExtensionDocumentWriter(IThreeMFExtension extension) : IThreeMFDocumentWriter {
    public Type DocumentType => extension.DocumentType;

    public void Write(Stream stream, ThreeMFDocument document, ThreeMFSerializerOptions options)
        => extension.Write(stream, document, options);
}
