namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents a sliced Bambu G-code 3MF document: a Bambu project that embeds the
/// sliced G-code of at least one plate, e.g. <c>Metadata/plate_1.gcode</c>.
/// </summary>
public sealed class BambuGCodeThreeMFDocument : BambuThreeMFDocument {
    /// <summary>
    /// Gets the parsed print job describing the embedded sliced plate G-code.
    /// </summary>
    public required BambuPrintJob PrintJob { get; init; }
}
