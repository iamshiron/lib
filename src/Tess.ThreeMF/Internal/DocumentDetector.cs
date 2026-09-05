namespace Shiron.Lib.Tess.ThreeMF.Internal;

/// <summary>
/// Selects the document handler for a package: probes every handler, then resolves the
/// winner deterministically by probe confidence and, at equal confidence, by the
/// specificity of the handlers' document types.
/// </summary>
internal static class DocumentDetector {
    public static IThreeMFDocumentHandler Select(IReadOnlyList<IThreeMFDocumentHandler> handlers, ThreeMFParseContext context) {
        ArgumentNullException.ThrowIfNull(handlers);
        ArgumentNullException.ThrowIfNull(context);

        var matches = handlers
            .Select(handler => (Handler: handler, Result: handler.Probe(context)))
            .Where(match => match.Result.IsMatch)
            .ToArray();

        if (matches.Length == 0)
            throw new ThreeMFDocumentDetectionException(
                "The package was not recognized as a 3MF document by any of the registered handlers: "
                + $"{string.Join(", ", handlers.Select(handler => Describe(handler.DocumentType)))}."
            );

        var survivors = matches
            .Where(match => !matches.Any(other =>
                !ReferenceEquals(other.Handler, match.Handler)
                && other.Result.Outranks(match.Result, other.Handler.DocumentType, match.Handler.DocumentType)))
            .ToArray();

        if (survivors.Length > 1)
            throw new ThreeMFAmbiguousDocumentTypeException(
                "The package was claimed with equal authority by unrelated document handlers: "
                + $"{string.Join(", ", survivors.Select(match => Describe(match.Handler.DocumentType)))}.",
                survivors.Select(match => match.Handler.DocumentType)
            );

        return survivors[0].Handler;
    }

    static string Describe(Type type) => type.FullName ?? type.ToString();
}
