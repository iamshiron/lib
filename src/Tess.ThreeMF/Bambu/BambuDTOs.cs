using System.Globalization;

namespace Shiron.Lib.Tess.ThreeMF.Bambu;

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

    /// <summary>
    /// Gets the objects declared in the model settings, keyed by their 3MF object id.
    /// </summary>
    public IReadOnlyDictionary<int, BambuModelObject> Objects { get; init; } = new Dictionary<int, BambuModelObject>();

    /// <summary>
    /// Gets the assembly items declared in the model settings.
    /// </summary>
    public IReadOnlyList<BambuAssemblyItem> AssemblyItems { get; init; } = [];
}

/// <summary>
/// Represents an object declaration in <c>Metadata/model_settings.config</c>.
/// </summary>
public sealed class BambuModelObject {
    /// <summary>Gets the 3MF object resource id.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the configured object name, or <see langword="null"/> when absent.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the object metadata keyed by its Bambu metadata key.</summary>
    public required IReadOnlyDictionary<string, string> Metadata { get; init; }

    /// <summary>Gets the object parts declared by the slicer.</summary>
    public IReadOnlyList<BambuModelPart> Parts { get; init; } = [];
}

/// <summary>
/// Represents one volume part of a Bambu model-settings object.
/// </summary>
public sealed class BambuModelPart {
    /// <summary>Gets the part id.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the slicer-specific part subtype, or <see langword="null"/> when absent.</summary>
    public string? Subtype { get; init; }

    /// <summary>Gets the part UUID, or <see langword="null"/> when absent.</summary>
    public string? Uuid { get; init; }

    /// <summary>Gets the part metadata keyed by its Bambu metadata key.</summary>
    public required IReadOnlyDictionary<string, string> Metadata { get; init; }
}

/// <summary>
/// Represents an item in the model-settings assembly section.
/// </summary>
public sealed class BambuAssemblyItem {
    /// <summary>Gets the referenced 3MF object resource id.</summary>
    public required int ObjectId { get; init; }

    /// <summary>Gets the instance id, or <see langword="null"/> for a volume-level item.</summary>
    public int? InstanceId { get; init; }

    /// <summary>Gets the volume id, or <see langword="null"/> for an instance-level item.</summary>
    public int? VolumeId { get; init; }

    /// <summary>Gets all assembly item attributes keyed by attribute name.</summary>
    public required IReadOnlyDictionary<string, string> Attributes { get; init; }
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
    /// Gets sliced plate details from <c>Metadata/slice_info.config</c>, or
    /// <see langword="null"/> when the package has no matching sliced plate entry.
    /// </summary>
    public BambuSlicePlate? SliceInfo { get; init; }

    /// <summary>Gets the sliced plate bounds and material summary, or <see langword="null"/> when absent.</summary>
    public BambuPlateDetails? Details { get; init; }

    /// <summary>Gets the sliced filament transition sequence, or <see langword="null"/> when absent.</summary>
    public BambuFilamentSequence? FilamentSequence { get; init; }

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
    /// Gets the decoded text of the plate's sliced G-code part, i.e. the content of
    /// <see cref="GCodePart"/>, or <see langword="null"/> when the plate is unsliced
    /// or carries no G-code. The text is exposed raw and verbatim; it is not parsed
    /// into a semantic G-code model.
    /// </summary>
    public string? GCode { get; init; }

    /// <summary>
    /// Gets the raw plate configuration map declared in
    /// <c>Metadata/model_settings.config</c>. Empty when the plate was detected from
    /// files only.
    /// </summary>
    public IReadOnlyDictionary<string, string> Config { get; init; } = new Dictionary<string, string>();
}

/// <summary>Represents the high-level contents of a Bambu plate bounds part.</summary>
public sealed class BambuPlateDetails {
    /// <summary>Gets the plate index.</summary>
    public required int Index { get; init; }

    /// <summary>Gets the bounds of all printed geometry, or <see langword="null"/> when absent.</summary>
    public BambuBounds? Bounds { get; init; }

    /// <summary>Gets bounds and print facts for individual objects, including the wipe tower when present.</summary>
    public IReadOnlyList<BambuPlateObjectBounds> Objects { get; init; } = [];

    /// <summary>Gets the selected bed type, or <see langword="null"/> when absent.</summary>
    public string? BedType { get; init; }

    /// <summary>Gets the selected filament colors.</summary>
    public IReadOnlyList<string> FilamentColors { get; init; } = [];

    /// <summary>Gets the selected filament indices.</summary>
    public IReadOnlyList<int> FilamentIds { get; init; } = [];

    /// <summary>Gets the first extruder index, or <see langword="null"/> when absent.</summary>
    public int? FirstExtruder { get; init; }

    /// <summary>Gets the first-layer duration in seconds, or <see langword="null"/> when absent.</summary>
    public double? FirstLayerTimeSeconds { get; init; }

    /// <summary>Gets whether the plate uses sequential printing.</summary>
    public bool? IsSequentialPrint { get; init; }

    /// <summary>Gets the selected nozzle diameter in millimeters, or <see langword="null"/> when absent.</summary>
    public double? NozzleDiameter { get; init; }
}

/// <summary>Represents the bounds and print facts of one object in a Bambu plate details part.</summary>
public sealed class BambuPlateObjectBounds {
    /// <summary>Gets the slicer-specific object identifier.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the object name, or <see langword="null"/> when absent.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the planar object bounds, or <see langword="null"/> when absent.</summary>
    public BambuBounds? Bounds { get; init; }

    /// <summary>Gets the object area, or <see langword="null"/> when absent.</summary>
    public double? Area { get; init; }

    /// <summary>Gets the layer height in millimeters, or <see langword="null"/> when absent.</summary>
    public double? LayerHeight { get; init; }
}

/// <summary>Represents planar bounds in millimeters.</summary>
public readonly record struct BambuBounds(double MinX, double MinY, double MaxX, double MaxY);

/// <summary>Represents the ordered filament choices emitted for a Bambu plate.</summary>
public sealed class BambuFilamentSequence {
    /// <summary>Gets the plate index.</summary>
    public required int Index { get; init; }

    /// <summary>Gets the nozzle selections in print order.</summary>
    public IReadOnlyList<int> NozzleSequence { get; init; } = [];

    /// <summary>Gets the optimized filament-to-slot assignment.</summary>
    public IReadOnlyList<int> OptimalAssignment { get; init; } = [];

    /// <summary>Gets the filament selections in print order.</summary>
    public IReadOnlyList<int> Sequence { get; init; } = [];
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

    /// <summary>
    /// Gets the model instance id, or <see langword="null"/> when the plate uses a
    /// compact object declaration instead of a <c>&lt;model_instance&gt;</c>.
    /// </summary>
    public int? InstanceId { get; init; }

    /// <summary>
    /// Gets the slicer-specific identify id, or <see langword="null"/> when absent.
    /// </summary>
    public int? IdentifyId { get; init; }
}

/// <summary>
/// Represents the sliced plate information in <c>Metadata/slice_info.config</c>.
/// </summary>
public sealed class BambuSlicePlate {
    /// <summary>Gets the plate index.</summary>
    public required int Index { get; init; }

    /// <summary>Gets the sliced plate metadata keyed by its Bambu metadata key.</summary>
    public required IReadOnlyDictionary<string, string> Config { get; init; }

    /// <summary>Gets objects included in the sliced output.</summary>
    public IReadOnlyList<BambuSlicedObject> Objects { get; init; } = [];

    /// <summary>Gets filament usage records for this plate.</summary>
    public IReadOnlyList<BambuFilament> Filaments { get; init; } = [];

    /// <summary>Gets the printer model identifier selected by the slicer.</summary>
    public string? PrinterModelId { get; init; }

    /// <summary>Gets the estimated print duration, or <see langword="null"/> when absent.</summary>
    public TimeSpan? EstimatedPrintTime { get; init; }

    /// <summary>Gets the estimated material weight in grams, or <see langword="null"/> when absent.</summary>
    public double? WeightGrams { get; init; }

    /// <summary>Gets the first-layer duration in seconds, or <see langword="null"/> when absent.</summary>
    public double? FirstLayerTimeSeconds { get; init; }

    /// <summary>Gets the configured nozzle diameters in millimeters.</summary>
    public IReadOnlyList<double> NozzleDiameters { get; init; } = [];

    /// <summary>Gets whether the sliced model extends outside the printable area.</summary>
    public bool? IsOutside { get; init; }

    /// <summary>Gets whether the sliced print uses support material.</summary>
    public bool? IsSupportUsed { get; init; }

    /// <summary>Gets the selected filament mapping for this plate.</summary>
    public IReadOnlyList<int> FilamentMap { get; init; } = [];

    /// <summary>Gets the nozzles participating in this sliced plate.</summary>
    public IReadOnlyList<BambuNozzle> Nozzles { get; init; } = [];

    /// <summary>Gets the configured AMS load and unload timings.</summary>
    public IReadOnlyList<BambuAmsTiming> AmsTimings { get; init; } = [];

    /// <summary>Gets the filament assignments for layer ranges.</summary>
    public IReadOnlyList<BambuLayerFilaments> LayerFilaments { get; init; } = [];
}

/// <summary>Represents a nozzle used by a sliced Bambu plate.</summary>
public sealed record BambuNozzle(int Id, int ExtruderId, double Diameter, string? VolumeType);

/// <summary>Represents a Bambu AMS type and its configured transfer timings.</summary>
public sealed record BambuAmsTiming(string? Type, double? LoadTimeSeconds, double? UnloadTimeSeconds);

/// <summary>Represents the filaments selected for one or more sliced layer ranges.</summary>
public sealed record BambuLayerFilaments(IReadOnlyList<int> FilamentIds, string LayerRanges);

/// <summary>
/// Represents an object in a sliced Bambu plate.
/// </summary>
public sealed class BambuSlicedObject {
    /// <summary>Gets the slicer-specific object identify id.</summary>
    public required int IdentifyId { get; init; }

    /// <summary>Gets the sliced object name, or <see langword="null"/> when absent.</summary>
    public string? Name { get; init; }

    /// <summary>Gets whether the object was skipped during slicing.</summary>
    public bool IsSkipped { get; init; }
}

/// <summary>
/// Represents a filament usage record from a sliced Bambu plate.
/// </summary>
public sealed class BambuFilament {
    /// <summary>Gets the filament id.</summary>
    public required int Id { get; init; }

    /// <summary>Gets all filament attributes keyed by attribute name.</summary>
    public required IReadOnlyDictionary<string, string> Properties { get; init; }

    /// <summary>Gets the AMS tray information identifier, or <see langword="null"/> when absent.</summary>
    public string? TrayInfoIndex => GetValue("tray_info_idx");

    /// <summary>Gets the material type, e.g. <c>PLA</c>, or <see langword="null"/> when absent.</summary>
    public string? MaterialType => GetValue("type");

    /// <summary>Gets the material color, e.g. <c>#DE4343</c>, or <see langword="null"/> when absent.</summary>
    public string? Color => GetValue("color");

    /// <summary>Gets the consumed filament length in meters, or <see langword="null"/> when absent or invalid.</summary>
    public double? UsedMeters => GetDouble("used_m");

    /// <summary>Gets the consumed filament weight in grams, or <see langword="null"/> when absent or invalid.</summary>
    public double? UsedGrams => GetDouble("used_g");

    /// <summary>Gets whether this filament was used for model geometry, or <see langword="null"/> when absent or invalid.</summary>
    public bool? IsUsedForObject => GetBoolean("used_for_object");

    /// <summary>Gets whether this filament was used for supports, or <see langword="null"/> when absent or invalid.</summary>
    public bool? IsUsedForSupport => GetBoolean("used_for_support");

    string? GetValue(string key) => Properties.TryGetValue(key, out var value) ? value : null;

    double? GetDouble(string key) {
        return GetValue(key) is { } value
            && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    bool? GetBoolean(string key) {
        return GetValue(key) is { } value && bool.TryParse(value, out var parsed) ? parsed : null;
    }
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

    /// <summary>
    /// Gets the decoded raw G-code text of every sliced plate, keyed by plate index,
    /// e.g. plate <c>1</c> to the text of <c>Metadata/plate_1.gcode</c>. Mirrors
    /// <see cref="GCodeParts"/>; never empty for a detected G-code document.
    /// </summary>
    public required IReadOnlyDictionary<int, string> GCodeByPlate { get; init; }
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

    /// <summary>
    /// Gets sliced plate details parsed from <c>Metadata/slice_info.config</c>.
    /// Empty when the package does not contain sliced plate information.
    /// </summary>
    public IReadOnlyDictionary<int, BambuSlicePlate> SlicePlates { get; init; } = new Dictionary<int, BambuSlicePlate>();

    /// <summary>Gets the cut records declared by the project.</summary>
    public IReadOnlyList<BambuCut> Cuts { get; init; } = [];
}

/// <summary>Represents one Bambu cut record for a model object.</summary>
public sealed record BambuCut(int ObjectId, int CutId, int Checksum, int ConnectorCount);

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

    /// <summary>Gets the Bambu Studio settings schema version, or <see langword="null"/> when absent.</summary>
    public string? Version { get; init; }

    /// <summary>Gets the selected print profile identifier, or <see langword="null"/> when absent.</summary>
    public string? PrintProfileId { get; init; }

    /// <summary>Gets the printer settings selected for the project.</summary>
    public BambuPrinterSettings Printer { get; init; } = new();

    /// <summary>Gets the printable bed polygon in millimeters.</summary>
    public IReadOnlyList<BambuPoint> PrintableArea { get; init; } = [];

    /// <summary>Gets the maximum printable height in millimeters, or <see langword="null"/> when absent.</summary>
    public double? PrintableHeight { get; init; }

    /// <summary>Gets the configured filament profiles, ordered by slicer filament index.</summary>
    public IReadOnlyList<BambuFilamentProfile> Filaments { get; init; } = [];

    /// <summary>Gets the purge volume matrix, or <see langword="null"/> when it is absent or malformed.</summary>
    public BambuPurgeMatrix? PurgeMatrix { get; init; }
}

/// <summary>Represents the printer configuration selected for a Bambu project.</summary>
public sealed class BambuPrinterSettings {
    /// <summary>Gets the printer model, or <see langword="null"/> when absent.</summary>
    public string? Model { get; init; }

    /// <summary>Gets the printer variant, or <see langword="null"/> when absent.</summary>
    public string? Variant { get; init; }

    /// <summary>Gets the printer profile identifier, or <see langword="null"/> when absent.</summary>
    public string? ProfileId { get; init; }

    /// <summary>Gets the printer technology, or <see langword="null"/> when absent.</summary>
    public string? Technology { get; init; }

    /// <summary>Gets the selected bed type, or <see langword="null"/> when absent.</summary>
    public string? BedType { get; init; }
}

/// <summary>Represents one filament profile configured by a Bambu project.</summary>
public sealed class BambuFilamentProfile {
    /// <summary>Gets the zero-based slicer filament index.</summary>
    public required int Index { get; init; }

    /// <summary>Gets the material type, or <see langword="null"/> when absent.</summary>
    public string? MaterialType { get; init; }

    /// <summary>Gets the filament color, or <see langword="null"/> when absent.</summary>
    public string? Color { get; init; }

    /// <summary>Gets the material profile identifier, or <see langword="null"/> when absent.</summary>
    public string? ProfileId { get; init; }

    /// <summary>Gets the AMS tray identifier, or <see langword="null"/> when absent.</summary>
    public string? TrayId { get; init; }

    /// <summary>Gets the filament vendor, or <see langword="null"/> when absent.</summary>
    public string? Vendor { get; init; }

    /// <summary>Gets the configured nozzle temperature in Celsius, or <see langword="null"/> when absent.</summary>
    public double? NozzleTemperature { get; init; }
}

/// <summary>Represents a two-dimensional point in millimeters.</summary>
public readonly record struct BambuPoint(double X, double Y);

/// <summary>Represents the dense row-major Bambu purge volume matrix in cubic millimeters.</summary>
public sealed class BambuPurgeMatrix {
    /// <summary>Gets the number of source and destination filament profiles.</summary>
    public required int Size { get; init; }

    /// <summary>Gets the row-major purge volumes.</summary>
    public required IReadOnlyList<double> Volumes { get; init; }

    /// <summary>Gets the purge volume from one filament index to another.</summary>
    public double GetVolume(int fromFilament, int toFilament) {
        if ((uint) fromFilament >= (uint) Size)
            throw new ArgumentOutOfRangeException(nameof(fromFilament));

        if ((uint) toFilament >= (uint) Size)
            throw new ArgumentOutOfRangeException(nameof(toFilament));

        return Volumes[(fromFilament * Size) + toFilament];
    }
}
