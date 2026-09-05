namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents a package-level relationship declared in the <c>_rels/.rels</c> part of a 3MF package.
/// </summary>
public sealed record Relationship {
    /// <summary>
    /// Gets the unique identifier of the relationship within its source part.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the relationship type URI, e.g.
    /// <c>http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel</c>.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the target part path of the relationship, e.g. <c>/3D/3dmodel.model</c>.
    /// </summary>
    public required string Target { get; init; }
}
