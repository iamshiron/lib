namespace Shiron.Lib.Tess.ThreeMF.Internal;

/// <summary>
/// Writes one specific kind of 3MF document into a ZIP archive on a stream. Writers
/// must be stateless; the serializer selects the writer whose document type is the
/// most specific ancestor of the document's runtime type.
/// </summary>
internal interface IThreeMFDocumentWriter {
    /// <summary>
    /// Gets the runtime document type this writer accepts. More derived types
    /// outrank their base types during writer selection.
    /// </summary>
    Type DocumentType { get; }

    /// <summary>
    /// Writes the document onto the given stream. Only invoked on the writer that
    /// won writer selection.
    /// </summary>
    /// <param name="stream">The stream to write to; it is left open.</param>
    /// <param name="document">The document to write.</param>
    /// <param name="options">The serialization options in effect.</param>
    void Write(Stream stream, ThreeMFDocument document, ThreeMFSerializerOptions options);
}
