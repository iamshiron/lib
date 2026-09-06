namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents the header items of a Bambu 3MF package, parsed from the
/// <c>Metadata/header_item</c> part in either its key/value text form or its XML
/// form, e.g. <c>&lt;header_item key="X-BBL-Client-Type" value="slicer"/&gt;</c>.
/// When that part is absent, the same <c>&lt;header_item&gt;</c> elements are read
/// from the <c>&lt;header&gt;</c> section of <c>Metadata/slice_info.config</c>.
/// </summary>
public sealed class BambuHeader {
    internal const string ClientTypeKey = "X-BBL-Client-Type";
    internal const string ClientVersionKey = "X-BBL-Client-Version";

    /// <summary>
    /// Gets all header items keyed by marker name, with their raw text values.
    /// Empty when the package does not contain the part.
    /// </summary>
    public required IReadOnlyDictionary<string, string> Items { get; init; }

    /// <summary>
    /// Gets the <c>X-BBL-Client-Type</c> item, e.g. <c>bambu-studio</c>, or
    /// <see langword="null"/> when absent.
    /// </summary>
    public string? ClientType => Items.TryGetValue(ClientTypeKey, out var value) ? value : null;

    /// <summary>
    /// Gets the <c>X-BBL-Client-Version</c> item, e.g. <c>01.09.05.51</c>, or
    /// <see langword="null"/> when absent.
    /// </summary>
    public string? ClientVersion => Items.TryGetValue(ClientVersionKey, out var value) ? value : null;
}

/// <summary>
/// Represents the model settings of a Bambu 3MF package, parsed from
/// <c>Metadata/model_settings.config</c>.
/// </summary>
public sealed class BambuModelSettings {
    /// <summary>
    /// Gets the raw configuration map of every plate declared in the config, keyed by
    /// plate index. Plate attributes and <c>&lt;metadata&gt;</c> children in both the
    /// <c>type</c>/element-text and <c>key</c>/<c>value</c>-attribute forms are
    /// merged into each map. Empty when the package does not contain the part.
    /// </summary>
    public required IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> PlateConfigs { get; init; }
}

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
    /// unsliced. Associated through the <c>gcode_file</c> plate metadata when
    /// declared, otherwise through the file naming convention.
    /// </summary>
    public string? GCodePart { get; init; }

    /// <summary>
    /// Gets the raw plate configuration map declared in
    /// <c>Metadata/model_settings.config</c>. Empty when the plate was detected from
    /// files only.
    /// </summary>
    public IReadOnlyDictionary<string, string> Config { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents one object placed on a Bambu print plate, referencing an object
/// resource of the 3MF core model.
/// </summary>
public sealed record BambuPlateObject {
    /// <summary>
    /// Gets the id of the referenced object resource in the 3MF core model.
    /// </summary>
    public required int ObjectId { get; init; }

    /// <summary>
    /// Gets the object name, or <see langword="null"/> when the plate declaration
    /// carries only the object id.
    /// </summary>
    public string? Name { get; init; }
}

/// <summary>
/// Represents the thumbnail images of a Bambu print plate.
/// </summary>
public sealed class BambuPlateThumbnail {
    /// <summary>
    /// Gets the part path of the plate image, e.g. <c>Metadata/plate_1.png</c>. When
    /// only a small image exists, this is the part path of the small image.
    /// </summary>
    public required string PartPath { get; init; }

    /// <summary>
    /// Gets the part path of the small plate image, e.g.
    /// <c>Metadata/plate_1_small.png</c>, or <see langword="null"/> when absent.
    /// </summary>
    public string? SmallPartPath { get; init; }

    /// <summary>
    /// Gets the raw image bytes of <see cref="PartPath"/>.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; init; }

    /// <summary>
    /// Gets the raw image bytes of <see cref="SmallPartPath"/>, or empty when absent.
    /// </summary>
    public ReadOnlyMemory<byte> SmallData { get; init; }
}

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

/// <summary>
/// Represents the producer-specific project metadata of a Bambu 3MF package.
/// </summary>
public sealed class BambuProject {
    /// <summary>
    /// Gets the application marker from the metadata of the 3D model part, e.g.
    /// <c>BambuStudio 01.09.05.51</c>, or <see langword="null"/> when the model
    /// declares none.
    /// </summary>
    public string? Application { get; init; }

    /// <summary>
    /// Gets the header items parsed from <c>Metadata/header_item</c>, in either the
    /// key/value text or the <c>&lt;header_item&gt;</c> XML form, falling back to the
    /// <c>&lt;header&gt;</c> section of <c>Metadata/slice_info.config</c> when the
    /// dedicated part is absent. Empty when the package contains neither part.
    /// </summary>
    public required BambuHeader Header { get; init; }

    /// <summary>
    /// Gets the slicer project settings parsed from
    /// <c>Metadata/project_settings.config</c>. Empty when the package does not
    /// contain the part.
    /// </summary>
    public required BambuProjectSettings ProjectSettings { get; init; }

    /// <summary>
    /// Gets the model settings parsed from <c>Metadata/model_settings.config</c>.
    /// Empty when the package does not contain the part.
    /// </summary>
    public required BambuModelSettings ModelSettings { get; init; }
}

/// <summary>
/// Represents the slicer project settings of a Bambu 3MF package, parsed from
/// <c>Metadata/project_settings.config</c>.
/// </summary>
/// <remarks>
/// Top-level scalar members are exposed in their text form; nested objects and arrays
/// keep their raw JSON text. The part is optional: an absent part yields empty settings.
/// </remarks>
public sealed class BambuProjectSettings {
    /// <summary>
    /// Gets the top-level settings by key. Null-valued members are skipped. Empty when
    /// the package does not contain the part.
    /// </summary>
    public required IReadOnlyDictionary<string, string> Values { get; init; }
}
