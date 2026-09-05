namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents one print plate of a Bambu 3MF package, merged from the plate
/// declarations in <c>Metadata/model_settings.config</c> and the
/// <c>Metadata/plate_&lt;index&gt;</c> files of the package.
/// </summary>
public sealed class BambuPlate {
    /// <summary>
    /// Gets the plate index, matching the <c>Metadata/plate_&lt;index&gt;</c> file naming.
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// Gets the plate name, or <see langword="null"/> when undeclared.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the objects placed on the plate. Empty when undeclared.
    /// </summary>
    public IReadOnlyList<BambuPlateObject> Objects { get; init; } = [];

    /// <summary>
    /// Gets the plate thumbnail, or <see langword="null"/> when the package contains
    /// no plate image for this index.
    /// </summary>
    public BambuPlateThumbnail? Thumbnail { get; init; }

    /// <summary>
    /// Gets the part path of the sliced G-code embedded for this plate, e.g.
    /// <c>Metadata/plate_1.gcode</c>, or <see langword="null"/> when the plate is
    /// unsliced.
    /// </summary>
    public string? GCodePart { get; init; }

    /// <summary>
    /// Gets the raw plate configuration map declared in
    /// <c>Metadata/model_settings.config</c>. Empty when the plate was detected from
    /// files only.
    /// </summary>
    public IReadOnlyDictionary<string, string> Config { get; init; } = new Dictionary<string, string>();
}
