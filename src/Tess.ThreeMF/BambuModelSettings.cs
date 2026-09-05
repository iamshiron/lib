namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents the model settings of a Bambu 3MF package, parsed from
/// <c>Metadata/model_settings.config</c>.
/// </summary>
public sealed class BambuModelSettings {
    /// <summary>
    /// Gets the raw configuration map of every plate declared in the config, keyed by
    /// plate index. Plate attributes and <c>&lt;metadata type="..."&gt;</c> children are
    /// merged into each map. Empty when the package does not contain the part.
    /// </summary>
    public required IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> PlateConfigs { get; init; }
}
