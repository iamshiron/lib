namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// The outcome of a document probe. Results with
/// <see cref="ThreeMFProbeConfidence.None"/> do not participate in document
/// detection; among matches, higher confidence wins, and equal confidence is resolved
/// by the specificity of the extensions' document types.
/// </summary>
public readonly record struct ThreeMFProbeResult(ThreeMFProbeConfidence Confidence) {
    /// <summary>
    /// Gets the result that claims nothing.
    /// </summary>
    public static ThreeMFProbeResult NoMatch => default;

    /// <summary>
    /// Gets a result with <see cref="ThreeMFProbeConfidence.Possible"/> confidence.
    /// </summary>
    public static ThreeMFProbeResult Possible => new(ThreeMFProbeConfidence.Possible);

    /// <summary>
    /// Gets a result with <see cref="ThreeMFProbeConfidence.Probable"/> confidence.
    /// </summary>
    public static ThreeMFProbeResult Probable => new(ThreeMFProbeConfidence.Probable);

    /// <summary>
    /// Gets a result with <see cref="ThreeMFProbeConfidence.Certain"/> confidence.
    /// </summary>
    public static ThreeMFProbeResult Certain => new(ThreeMFProbeConfidence.Certain);

    /// <summary>
    /// Gets a value indicating whether the extension claims the package at all.
    /// </summary>
    public bool IsMatch => Confidence is not ThreeMFProbeConfidence.None;
}
