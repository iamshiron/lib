using Shiron.Lib.Tess.ThreeMF.Exceptions;

namespace Shiron.Lib.Tess.ThreeMF.Detection;

/// <summary>
/// Selects the document handler for a package: probes every handler, then resolves the
/// winner deterministically by probe confidence and, at equal confidence, by the
/// specificity of the handlers' document types.
/// </summary>
internal static class DocumentDetector {
    public static IThreeMFDocumentHandler Select(IReadOnlyList<IThreeMFDocumentHandler> handlers, ThreeMFProbeContext context) {
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
                && Outranks(other.Result, other.Handler.DocumentType, match.Result, match.Handler.DocumentType)))
            .ToArray();

        if (survivors.Length > 1)
            throw new ThreeMFAmbiguousDocumentTypeException(
                "The package was claimed with equal authority by unrelated document handlers: "
                + $"{string.Join(", ", survivors.Select(match => Describe(match.Handler.DocumentType)))}.",
                survivors.Select(match => match.Handler.DocumentType)
            );

        return survivors[0].Handler;
    }

    /// <summary>
    /// Determines whether one match strictly outranks another: by confidence first, and
    /// by document-type specificity second, where a strictly more derived document type
    /// outranks its own base types. Matches that neither outrank each other are tied.
    /// </summary>
    /// <param name="winner">The probe result of the potentially outranking handler.</param>
    /// <param name="winnerDocumentType">The document type of the potentially outranking handler.</param>
    /// <param name="loser">The probe result of the competing handler.</param>
    /// <param name="loserDocumentType">The document type of the competing handler.</param>
    /// <returns><see langword="true"/> when the first match strictly outranks the second.</returns>
    static bool Outranks(in ThreeMFProbeResult winner, Type winnerDocumentType, in ThreeMFProbeResult loser, Type loserDocumentType) {
        if (winner.Confidence != loser.Confidence)
            return winner.Confidence > loser.Confidence;

        return winnerDocumentType != loserDocumentType
            && loserDocumentType.IsAssignableFrom(winnerDocumentType);
    }

    static string Describe(Type type) => type.FullName ?? type.ToString();
}
