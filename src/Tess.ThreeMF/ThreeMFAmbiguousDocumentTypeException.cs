namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Thrown when multiple unrelated document handlers claim a package with equal
/// authority, so no single runtime document type can be determined deterministically.
/// </summary>
public sealed class ThreeMFAmbiguousDocumentTypeException : ThreeMFDocumentDetectionException {
    /// <summary>
    /// Gets the document types of the handlers that claimed the package with equal authority.
    /// </summary>
    public IReadOnlyList<Type> CandidateTypes { get; }

    public ThreeMFAmbiguousDocumentTypeException(string message, IEnumerable<Type> candidateTypes) : base(message) {
        ArgumentNullException.ThrowIfNull(candidateTypes);

        CandidateTypes = candidateTypes.ToArray();
    }
}
