namespace Shiron.Lib.Tess.ThreeMF;

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
