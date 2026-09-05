namespace Shiron.Lib.Tess.ThreeMF.Internal;

/// <summary>
/// The outcome of a document handler probe. Probes with
/// <see cref="ThreeMFConfidence.None"/> do not participate in document detection;
/// among matches, higher confidence wins, and equal confidence is resolved by the
/// specificity of the handlers' document types.
/// </summary>
internal readonly record struct ThreeMFProbeResult(ThreeMFConfidence Confidence) {
    public static ThreeMFProbeResult NoMatch => default;

    public static ThreeMFProbeResult Possible => new(ThreeMFConfidence.Possible);

    public static ThreeMFProbeResult Likely => new(ThreeMFConfidence.Likely);

    public static ThreeMFProbeResult Authoritative => new(ThreeMFConfidence.Authoritative);

    /// <summary>
    /// Gets a value indicating whether the handler claims the package at all.
    /// </summary>
    public bool IsMatch => Confidence is not ThreeMFConfidence.None;

    /// <summary>
    /// Determines whether this result outranks another: by confidence first, and by
    /// document-type specificity second, where a strictly more derived document type
    /// outranks its own base types. Results that neither outrank each other are tied.
    /// </summary>
    /// <param name="other">The competing probe result.</param>
    /// <param name="documentType">The document type of the handler owning this result.</param>
    /// <param name="otherDocumentType">The document type of the competing handler.</param>
    /// <returns><see langword="true"/> when this result strictly outranks the other.</returns>
    public bool Outranks(in ThreeMFProbeResult other, Type documentType, Type otherDocumentType) {
        if (Confidence != other.Confidence)
            return Confidence > other.Confidence;

        return documentType != otherDocumentType
            && otherDocumentType.IsAssignableFrom(documentType);
    }
}
