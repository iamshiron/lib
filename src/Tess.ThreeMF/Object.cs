namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents an <c>&lt;object&gt;</c> resource of a 3MF model: either a mesh or a
/// composition of components referencing other objects.
/// </summary>
public sealed class Object {
    /// <summary>
    /// Gets the unique positive resource id of the object within the model.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the purpose of the object. Defaults to <see cref="ObjectType.Model"/>.
    /// </summary>
    public ObjectType Type { get; init; } = ObjectType.Model;

    /// <summary>
    /// Gets the object name,
    /// or <see langword="null"/> when the object omits the attribute.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the object part number,
    /// or <see langword="null"/> when the object omits the attribute.
    /// </summary>
    public string? PartNumber { get; init; }

    /// <summary>
    /// Gets the mesh geometry of the object,
    /// or <see langword="null"/> when the object is composed of components instead.
    /// </summary>
    public Mesh? Mesh { get; init; }

    /// <summary>
    /// Gets the component references of the object. Empty for mesh objects.
    /// </summary>
    public IReadOnlyList<Component> Components { get; init; } = [];
}
