namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents the <c>&lt;build&gt;</c> section of a 3MF model: the items to fabricate.
/// </summary>
public sealed class Build {
    /// <summary>
    /// Gets the build items. Empty when the model builds nothing.
    /// </summary>
    public IReadOnlyList<BuildItem> Items { get; init; } = [];
}
