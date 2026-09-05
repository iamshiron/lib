namespace Shiron.Lib.Tess.ThreeMF;

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
    /// key/value text or the <c>&lt;header_item&gt;</c> XML form. Empty when the
    /// package does not contain the part.
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
