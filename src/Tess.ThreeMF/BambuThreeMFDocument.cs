namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents a Bambu (Bambu Studio) project 3MF document: the generic 3MF core view
/// plus the producer-specific project metadata parsed from the <c>Metadata/</c> parts
/// of the package. The raw package files remain available through
/// <see cref="ThreeMFDocument.Files"/>.
/// </summary>
public class BambuThreeMFDocument : ThreeMFDocument {
    /// <summary>
    /// Gets the parsed Bambu project metadata: header items, project settings, and
    /// model settings.
    /// </summary>
    public required BambuProject Project { get; init; }

    /// <summary>
    /// Gets the print plates of the package, ordered by plate index. Empty when the
    /// package declares no plates.
    /// </summary>
    public required IReadOnlyList<BambuPlate> Plates { get; init; }
}
