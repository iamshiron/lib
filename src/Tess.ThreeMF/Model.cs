namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents the contents of the <c>&lt;model&gt;</c> root element of a 3MF core
/// model part.
/// </summary>
public sealed class Model {
    /// <summary>
    /// Gets the unit of measure of the model geometry.
    /// Defaults to <see cref="Unit.Millimeter"/>.
    /// </summary>
    public Unit Unit { get; init; } = Unit.Millimeter;

    /// <summary>
    /// Gets the <c>xml:lang</c> language tag of the model,
    /// or <see langword="null"/> when the model omits the attribute.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Gets the metadata entries of the model. Empty when the model declares none.
    /// </summary>
    public IReadOnlyList<Metadata> Metadata { get; init; } = [];

    /// <summary>
    /// Gets the object resources of the model.
    /// </summary>
    public required Resources Resources { get; init; }

    /// <summary>
    /// Gets the build section of the model. Empty when the model builds nothing.
    /// </summary>
    public Build Build { get; init; } = new();
}
