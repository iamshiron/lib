namespace Shiron.Lib.Tess.ThreeMF.Bambu.Parsing;

/// <summary>
/// One plate declaration parsed from <c>Metadata/model_settings.config</c>: the
/// resolved index, name, and objects, plus the raw configuration map of the plate
/// element.
/// </summary>
internal sealed record BambuPlateConfig(
    int Index,
    string? Name,
    IReadOnlyList<BambuPlateObject> Objects,
    IReadOnlyDictionary<string, string> Raw
);
