namespace Shiron.Lib.Tess.ThreeMF.Detection;

/// <summary>
/// The outcome of a document probe. Results with
/// <see cref="ProbeConfidence.None"/> do not participate in document
/// detection; among matches, higher confidence wins, and equal confidence is resolved
/// by the specificity of the extensions' document types.
/// </summary>
public readonly record struct ProbeResult(ProbeConfidence Confidence) {
    /// <summary>
    /// Gets the result that claims nothing.
    /// </summary>
    public static ProbeResult NoMatch => default;

    /// <summary>
    /// Gets a result with <see cref="ProbeConfidence.Possible"/> confidence.
    /// </summary>
    public static ProbeResult Possible => new(ProbeConfidence.Possible);

    /// <summary>
    /// Gets a result with <see cref="ProbeConfidence.Probable"/> confidence.
    /// </summary>
    public static ProbeResult Probable => new(ProbeConfidence.Probable);

    /// <summary>
    /// Gets a result with <see cref="ProbeConfidence.Certain"/> confidence.
    /// </summary>
    public static ProbeResult Certain => new(ProbeConfidence.Certain);

    /// <summary>
    /// Gets a value indicating whether the extension claims the package at all.
    /// </summary>
    public bool IsMatch => Confidence is not ProbeConfidence.None;
}
