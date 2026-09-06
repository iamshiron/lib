namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents the parsed 3MF core models of a package: the model part selected via the
/// OPC package relationships or content types plus, recursively, the model parts
/// linked through 3D model relationships of their part-level relationships parts,
/// decoded into core specification types.
/// </summary>
public sealed class Core {
    /// <summary>
    /// Gets the normalized part path of the main model part inside the package,
    /// e.g. <c>3D/3dmodel.model</c>.
    /// </summary>
    public required string PartPath { get; init; }

    /// <summary>
    /// Gets all models parsed from the package's model parts: the main model first,
    /// followed by the relationship-linked model parts in deterministic depth-first
    /// relationship declaration order. The 3MF core specification defines a single
    /// model part; the production extension introduces additional linked model parts.
    /// </summary>
    public required IReadOnlyList<Model> Models { get; init; }

    /// <summary>
    /// Gets the main model of the package: the model of the part selected via the 3D
    /// model relationship, or via the model content type when no relationship exists.
    /// Always identical to the first entry of <see cref="Models"/>.
    /// </summary>
    public required Model MainModel { get; init; }

    /// <summary>
    /// Gets the parsed models keyed by their normalized package part path, including
    /// the main model part. Resolves the <see cref="Component.PartPath"/> of
    /// production-extension component references.
    /// </summary>
    public required IReadOnlyDictionary<string, Model> Parts { get; init; }
}
