namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents the parsed 3MF core models of a package: the model parts selected via the
/// OPC package relationships or content types, decoded into core specification types.
/// </summary>
public sealed class Core {
    /// <summary>
    /// Gets the normalized part path of the main model part inside the package,
    /// e.g. <c>3D/3dmodel.model</c>.
    /// </summary>
    public required string PartPath { get; init; }

    /// <summary>
    /// Gets all models parsed from the package's model parts. The 3MF core
    /// specification defines a single model part, so this contains exactly one entry;
    /// extensions may introduce additional model parts.
    /// </summary>
    public required IReadOnlyList<Model> Models { get; init; }

    /// <summary>
    /// Gets the main model of the package: the model of the part selected via the 3D
    /// model relationship, or via the model content type when no relationship exists.
    /// Always identical to the first entry of <see cref="Models"/>.
    /// </summary>
    public required Model MainModel { get; init; }
}
