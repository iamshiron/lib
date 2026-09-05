namespace Shiron.Lib.Tess.ThreeMF.Internal;

/// <summary>
/// Selects the document writer for a document: resolves the most specific registered
/// writer for the document's runtime type by walking its class hierarchy upwards, so
/// e.g. a <see cref="BambuGCodeThreeMFDocument"/> picks the G-code writer over the
/// Bambu and standard writers. The built-in writers always participate and win over
/// extension writers registered for the same document type; among extensions, the
/// first registered writer for a document type wins.
/// </summary>
internal static class DocumentWriterRegistry {
    static readonly IReadOnlyList<IThreeMFDocumentWriter> BuiltInWriters = [
        new BambuGCodeDocumentWriter(),
        new BambuDocumentWriter(),
        new StandardDocumentWriter(),
    ];

    public static IThreeMFDocumentWriter Select(Type documentType, IReadOnlyList<IThreeMFDocumentWriter> extensionWriters) {
        ArgumentNullException.ThrowIfNull(documentType);
        ArgumentNullException.ThrowIfNull(extensionWriters);

        var writers = BuiltInWriters.Concat(extensionWriters);

        for (var type = documentType; type is not null; type = type.BaseType) {
            if (writers.FirstOrDefault(writer => writer.DocumentType == type) is { } match)
                return match;
        }

        throw new InvalidOperationException(
            $"No registered 3MF document writer accepts the document type '{documentType.FullName}'."
        );
    }
}
