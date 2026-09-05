namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents the sliced print job of a Bambu G-code 3MF package.
/// </summary>
public sealed class BambuPrintJob {
    /// <summary>
    /// Gets the package part paths of the embedded sliced plate G-code files, e.g.
    /// <c>Metadata/plate_1.gcode</c>, ordered by plate index. Never empty for a
    /// detected G-code document.
    /// </summary>
    public required IReadOnlyList<string> GCodeParts { get; init; }
}
